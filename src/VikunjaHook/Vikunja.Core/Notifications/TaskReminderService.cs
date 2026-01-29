using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Providers;

namespace Vikunja.Core.Notifications;

public class TaskReminderService : IDisposable
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly JsonFileConfigurationManager _configManager;
    private readonly SimpleTemplateEngine _templateEngine;
    private readonly IEnumerable<PushDeerProvider> _providers;
    private readonly InMemoryPushEventHistory _pushHistory;
    private readonly TaskReminderHistory _reminderHistory;
    private readonly ILogger<TaskReminderService> _logger;
    private readonly string? _vikunjaUrl;
    
    // Blacklist: taskId_type -> reminder info (when it was reminded and expiry time)
    private readonly ConcurrentDictionary<string, BlacklistEntry> _remindedTasks = new();
    private const int MaxBlacklistSize = 10000; // 防止内存泄漏的最大条目数
    
    private Timer? _timer;
    private Timer? _cleanupTimer;
    private bool _isScanning = false;

    // 黑名单条目
    private record BlacklistEntry(DateTime RemindedAt, DateTime ExpiresAt);

    private bool ShouldSendReminder(UserConfig config, VikunjaTask task)
    {
        if (config.ReminderConfig!.EnableLabelFilter && 
            config.ReminderConfig.FilterLabelIds.Any())
        {
            var hasMatchingLabel = task.Labels != null && 
                task.Labels.Any(label => config.ReminderConfig.FilterLabelIds.Contains(label.Id));
            
            if (!hasMatchingLabel)
            {
                return false;
            }
        }
        
        return true;
    }

    public TaskReminderService(
        IVikunjaClientFactory clientFactory,
        JsonFileConfigurationManager configManager,
        SimpleTemplateEngine templateEngine,
        IEnumerable<PushDeerProvider> providers,
        InMemoryPushEventHistory pushHistory,
        TaskReminderHistory reminderHistory,
        ILogger<TaskReminderService> logger,
        string? vikunjaUrl = null)
    {
        _clientFactory = clientFactory;
        _configManager = configManager;
        _templateEngine = templateEngine;
        _providers = providers;
        _pushHistory = pushHistory;
        _reminderHistory = reminderHistory;
        _logger = logger;
        _vikunjaUrl = vikunjaUrl;
    }

    public void Start()
    {
        _logger.LogInformation("Starting TaskReminderService");
        
        // Start with default 10 second interval, will adjust based on user configs
        _timer = new Timer(async _ => await ScanTasksAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        // Start cleanup timer - runs every 10 minutes to clean up expired entries
        _cleanupTimer = new Timer(_ => CleanupBlacklist(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }
    
    private void CleanupBlacklist()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _remindedTasks
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _remindedTasks.TryRemove(key, out _);
            }
            
            // 如果黑名单过大，移除最旧的条目
            if (_remindedTasks.Count > MaxBlacklistSize)
            {
                var toRemove = _remindedTasks
                    .OrderBy(kvp => kvp.Value.RemindedAt)
                    .Take(_remindedTasks.Count - MaxBlacklistSize)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in toRemove)
                {
                    _remindedTasks.TryRemove(key, out _);
                }
                
                _logger.LogWarning("Blacklist size exceeded {MaxSize}, removed {Count} oldest entries", 
                    MaxBlacklistSize, toRemove.Count);
            }
            
            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired blacklist entries, current size: {Size}", 
                    expiredKeys.Count, _remindedTasks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up blacklist");
        }
    }

    private async Task ScanTasksAsync()
    {
        if (_isScanning)
        {
            return; // Skip if already scanning
        }

        _isScanning = true;
        
        try
        {
            var configs = await _configManager.LoadAllConfigsAsync(CancellationToken.None);
            
            // Find minimum scan interval from all enabled configs
            var minInterval = 10;
            var enabledConfigs = configs.Where(c => c.ReminderConfig?.Enabled == true).ToList();
            
            if (enabledConfigs.Any())
            {
                minInterval = enabledConfigs.Min(c => c.ReminderConfig!.ScanIntervalSeconds);
                
                // Update timer interval if needed
                if (_timer != null)
                {
                    _timer.Change(TimeSpan.FromSeconds(minInterval), TimeSpan.FromSeconds(minInterval));
                }
            }
            
            // Clean up old blacklist entries (older than 1 hour) - 快速清理
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var oldKeys = _remindedTasks
                .Where(kvp => kvp.Value.ExpiresAt < oneHourAgo)
                .Select(kvp => kvp.Key)
                .Take(100) // 每次最多清理100个，避免阻塞
                .ToList();
            
            foreach (var key in oldKeys)
            {
                _remindedTasks.TryRemove(key, out _);
            }
            
            // Get all projects to scan tasks
            List<VikunjaProject> projects;
            try
            {
                projects = await _clientFactory.GetAsync<List<VikunjaProject>>("projects", CancellationToken.None) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get projects for task reminder scan");
                return;
            }
            
            var now = DateTime.UtcNow;
            var fiveMinutesAgo = now.AddMinutes(-5);
            var fiveMinutesLater = now.AddMinutes(5);
            
            // Scan tasks in each project
            foreach (var project in projects)
            {
                try
                {
                    var tasks = await _clientFactory.GetAsync<List<VikunjaTask>>(
                        $"projects/{project.Id}/tasks?per_page=100", 
                        CancellationToken.None) ?? new();
                    
                    foreach (var task in tasks)
                    {
                        // Skip completed tasks
                        if (task.Done)
                        {
                            continue;
                        }
                        
                        // Check start date (within past 5 minutes to future 5 minutes)
                        if (task.StartDate.HasValue && 
                            task.StartDate.Value >= fiveMinutesAgo && 
                            task.StartDate.Value <= fiveMinutesLater)
                        {
                            var reminderTime = task.StartDate.Value.ToString("yyyy-MM-dd HH:mm");
                            var blacklistKey = $"{task.Id}_start_{reminderTime}";
                            
                            if (!_remindedTasks.ContainsKey(blacklistKey))
                            {
                                foreach (var config in enabledConfigs)
                                {
                                    if (ShouldSendReminder(config, task))
                                    {
                                        await SendReminderAsync(config, task, project, "start");
                                    }
                                }
                                
                                var expiresAt = DateTime.UtcNow.AddHours(2);
                                _remindedTasks.TryAdd(blacklistKey, new BlacklistEntry(DateTime.UtcNow, expiresAt));
                                
                                _logger.LogInformation("Sent start reminder for task {TaskId}, blacklist size: {Size}", 
                                    task.Id, _remindedTasks.Count);
                            }
                        }
                        
                        // Check due date (within past 5 minutes to future 5 minutes)
                        if (task.DueDate.HasValue && 
                            task.DueDate.Value >= fiveMinutesAgo && 
                            task.DueDate.Value <= fiveMinutesLater)
                        {
                            var reminderTime = task.DueDate.Value.ToString("yyyy-MM-dd HH:mm");
                            var blacklistKey = $"{task.Id}_due_{reminderTime}";
                            
                            if (!_remindedTasks.ContainsKey(blacklistKey))
                            {
                                foreach (var config in enabledConfigs)
                                {
                                    if (ShouldSendReminder(config, task))
                                    {
                                        await SendReminderAsync(config, task, project, "due");
                                    }
                                }
                                
                                var expiresAt = DateTime.UtcNow.AddHours(2);
                                _remindedTasks.TryAdd(blacklistKey, new BlacklistEntry(DateTime.UtcNow, expiresAt));
                                
                                _logger.LogInformation("Sent due reminder for task {TaskId}, blacklist size: {Size}", 
                                    task.Id, _remindedTasks.Count);
                            }
                        }
                        
                        // Check reminders (within past 5 minutes to future 5 minutes)
                        if (task.Reminders != null && task.Reminders.Any())
                        {
                            foreach (var reminder in task.Reminders)
                            {
                                if (reminder.Reminder >= fiveMinutesAgo && 
                                    reminder.Reminder <= fiveMinutesLater)
                                {
                                    var reminderTime = reminder.Reminder.ToString("yyyy-MM-dd HH:mm");
                                    var blacklistKey = $"{task.Id}_reminder_{reminderTime}";
                                    
                                    if (!_remindedTasks.ContainsKey(blacklistKey))
                                    {
                                        foreach (var config in enabledConfigs)
                                        {
                                            if (ShouldSendReminder(config, task))
                                            {
                                                await SendReminderAsync(config, task, project, "reminder");
                                            }
                                        }
                                        
                                        var expiresAt = DateTime.UtcNow.AddHours(2);
                                        _remindedTasks.TryAdd(blacklistKey, new BlacklistEntry(DateTime.UtcNow, expiresAt));
                                        
                                        _logger.LogInformation("Sent reminder for task {TaskId}, blacklist size: {Size}", 
                                            task.Id, _remindedTasks.Count);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan tasks in project {ProjectId}", project.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in task reminder scan");
        }
        finally
        {
            _isScanning = false;
        }
    }

    private async Task SendReminderAsync(UserConfig config, VikunjaTask task, VikunjaProject project, string reminderType)
    {
        try
        {
            var reminderConfig = config.ReminderConfig!;
            
            // Select template based on reminder type
            var template = reminderType switch
            {
                "start" => reminderConfig.StartDateTemplate,
                "due" => reminderConfig.DueDateTemplate,
                "reminder" => reminderConfig.ReminderTimeTemplate,
                _ => reminderConfig.ReminderTimeTemplate
            };
            
            // Build context for template
            var context = new TemplateContext
            {
                Task = new TaskTemplateData
                {
                    Id = (int)task.Id,
                    Title = task.Title ?? string.Empty,
                    Description = task.Description ?? string.Empty,
                    Done = task.Done,
                    Priority = task.Priority,
                    DueDate = task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/tasks/{task.Id}" 
                        : $"Task ID: {task.Id}"
                },
                Project = new ProjectTemplateData
                {
                    Id = (int)project.Id,
                    Title = project.Title ?? string.Empty,
                    Description = project.Description ?? string.Empty,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/projects/{project.Id}" 
                        : $"Project ID: {project.Id}"
                },
                Event = new EventData
                {
                    Type = $"task.reminder.{reminderType}",
                    Timestamp = DateTime.UtcNow,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/tasks/{task.Id}" 
                        : $"Task ID: {task.Id}"
                }
            };
            
            // Add custom properties for reminder-specific data
            var startDate = task.StartDate?.ToString("yyyy-MM-dd HH:mm") ?? "None";
            var endDate = task.EndDate?.ToString("yyyy-MM-dd HH:mm") ?? "None";
            var reminders = task.Reminders != null && task.Reminders.Any() 
                ? string.Join(", ", task.Reminders.Select(r => r.Reminder.ToString("yyyy-MM-dd HH:mm")))
                : "None";
            
            var title = _templateEngine.Render(template.TitleTemplate, context)
                .Replace("{{task.startDate}}", startDate)
                .Replace("{{task.endDate}}", endDate)
                .Replace("{{task.reminders}}", reminders)
                .Replace("{{reminder.type}}", reminderType);
                
            var body = _templateEngine.Render(template.BodyTemplate, context)
                .Replace("{{task.startDate}}", startDate)
                .Replace("{{task.endDate}}", endDate)
                .Replace("{{task.reminders}}", reminders)
                .Replace("{{reminder.type}}", reminderType);
            
            var message = new NotificationMessage(title, body, reminderConfig.Format);
            
            // Determine which providers to use
            var providerTypes = reminderConfig.Providers.Any() 
                ? reminderConfig.Providers 
                : config.DefaultProviders;
            
            if (!providerTypes.Any())
            {
                _logger.LogWarning("No providers configured for user {UserId}", config.UserId);
                return;
            }
            
            // Send to each provider
            foreach (var providerType in providerTypes)
            {
                var providerConfig = config.Providers.FirstOrDefault(p => 
                    p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));
                
                if (providerConfig == null)
                {
                    continue;
                }
                
                var provider = _providers.FirstOrDefault(p => 
                    p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));
                
                if (provider == null)
                {
                    continue;
                }
                
                try
                {
                    NotificationResult result;
                    
                    if (provider is PushDeerProvider pushDeer && 
                        providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
                    {
                        result = await pushDeer.SendAsync(message, pushKey, CancellationToken.None);
                    }
                    else
                    {
                        result = await provider.SendAsync(message, CancellationToken.None);
                    }
                    
                    // Record in push history
                    _pushHistory.AddRecord(new PushEventRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventName = $"task.reminder.{reminderType}",
                        Timestamp = DateTime.UtcNow,
                        EventData = new EventDataInfo
                        {
                            Title = title,
                            Body = body,
                            Format = reminderConfig.Format.ToString()
                        },
                        Providers = new List<ProviderPushResult>
                        {
                            new ProviderPushResult
                            {
                                ProviderType = providerType,
                                Success = result.Success,
                                Message = result.ErrorMessage,
                                Timestamp = DateTime.UtcNow,
                                NotificationContent = message
                            }
                        }
                    });
                    
                    // Record in reminder history
                    _reminderHistory.AddRecord(new TaskReminderRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        TaskId = task.Id,
                        TaskTitle = task.Title ?? string.Empty,
                        ProjectTitle = project.Title ?? string.Empty,
                        ReminderType = reminderType,
                        UserId = config.UserId,
                        Title = title,
                        Body = body,
                        Providers = new List<string> { providerType },
                        Success = result.Success,
                        ErrorMessage = result.ErrorMessage
                    });
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("Reminder sent to {UserId} via {Provider} for task {TaskId}", 
                            config.UserId, providerType, task.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send reminder to {UserId} via {Provider}: {Error}", 
                            config.UserId, providerType, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder via {Provider}", providerType);
                    
                    // Record failed reminder
                    _reminderHistory.AddRecord(new TaskReminderRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        TaskId = task.Id,
                        TaskTitle = task.Title ?? string.Empty,
                        ProjectTitle = project.Title ?? string.Empty,
                        ReminderType = reminderType,
                        UserId = config.UserId,
                        Title = title,
                        Body = body,
                        Providers = new List<string> { providerType },
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for task {TaskId}", task.Id);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cleanupTimer?.Dispose();
        _remindedTasks.Clear();
    }
    
    // 获取黑名单状态（用于监控和调试）
    public BlacklistStatus GetBlacklistStatus()
    {
        var now = DateTime.UtcNow;
        var entries = _remindedTasks.Select(kvp => new BlacklistEntryInfo
        {
            Key = kvp.Key,
            RemindedAt = kvp.Value.RemindedAt,
            ExpiresAt = kvp.Value.ExpiresAt,
            IsExpired = kvp.Value.ExpiresAt < now
        }).ToList();
        
        return new BlacklistStatus
        {
            TotalEntries = _remindedTasks.Count,
            MaxSize = MaxBlacklistSize,
            ExpiredEntries = entries.Count(e => e.IsExpired),
            Entries = entries.OrderByDescending(e => e.RemindedAt).Take(100).ToList()
        };
    }
}

// 黑名单状态响应
public record BlacklistStatus
{
    public int TotalEntries { get; init; }
    public int MaxSize { get; init; }
    public int ExpiredEntries { get; init; }
    public List<BlacklistEntryInfo> Entries { get; init; } = new();
}

public record BlacklistEntryInfo
{
    public string Key { get; init; } = "";
    public DateTime RemindedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}


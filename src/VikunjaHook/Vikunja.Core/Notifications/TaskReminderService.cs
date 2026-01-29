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
    
    // 内存中的待提醒任务: taskId -> TaskReminderInfo
    private readonly ConcurrentDictionary<long, TaskReminderInfo> _pendingReminders = new();
    
    // 已发送提醒的记录: "taskId_type_time" -> 发送时间
    private readonly ConcurrentDictionary<string, DateTime> _sentReminders = new();
    private const int MaxSentRemindersSize = 10000;
    
    private Timer? _checkTimer;
    private Timer? _cleanupTimer;
    private bool _isInitialized = false;

    // 任务提醒信息
    private record TaskReminderInfo(
        long TaskId,
        string Title,
        long ProjectId,
        string ProjectTitle,
        DateTime? StartDate,
        DateTime? DueDate,
        DateTime? EndDate,
        List<DateTime> Reminders,
        List<long> LabelIds
    );

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
        _logger.LogInformation("Starting TaskReminderService with webhook-based memory management");
        
        // 启动时扫描一次所有任务初始化内存
        Task.Run(async () => await InitializeTasksAsync());
        
        // 启动定时检查器 - 每10秒检查一次内存中的任务
        _checkTimer = new Timer(async _ => await CheckPendingRemindersAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        
        // 启动清理定时器 - 每小时清理一次已发送记录
        _cleanupTimer = new Timer(_ => CleanupSentReminders(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }
    
    // 初始化：扫描所有任务加载到内存
    private async Task InitializeTasksAsync()
    {
        if (_isInitialized)
        {
            return;
        }
        
        try
        {
            _logger.LogInformation("Initializing task reminders - scanning all tasks");
            
            var projects = await _clientFactory.GetAsync<List<VikunjaProject>>("projects", CancellationToken.None) ?? new();
            
            foreach (var project in projects)
            {
                try
                {
                    var tasks = await _clientFactory.GetAsync<List<VikunjaTask>>(
                        $"projects/{project.Id}/tasks?per_page=100", 
                        CancellationToken.None) ?? new();
                    
                    foreach (var task in tasks)
                    {
                        if (!task.Done)
                        {
                            UpdateTaskInMemory(task, project);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load tasks from project {ProjectId}", project.Id);
                }
            }
            
            _isInitialized = true;
            _logger.LogInformation("Task reminder initialization complete - loaded {Count} pending tasks", _pendingReminders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize task reminders");
        }
    }
    
    // 通过 webhook 更新内存中的任务
    public void OnTaskCreated(VikunjaTask task, VikunjaProject project)
    {
        if (!task.Done)
        {
            UpdateTaskInMemory(task, project);
            _logger.LogInformation("Task {TaskId} added to reminder memory", task.Id);
        }
    }
    
    public void OnTaskUpdated(VikunjaTask task, VikunjaProject project)
    {
        if (task.Done)
        {
            // 任务完成，从内存中移除
            _pendingReminders.TryRemove(task.Id, out _);
            _logger.LogInformation("Task {TaskId} removed from reminder memory (completed)", task.Id);
        }
        else
        {
            // 更新任务信息
            UpdateTaskInMemory(task, project);
            _logger.LogInformation("Task {TaskId} updated in reminder memory", task.Id);
        }
    }
    
    public void OnTaskDeleted(long taskId)
    {
        _pendingReminders.TryRemove(taskId, out _);
        _logger.LogInformation("Task {TaskId} removed from reminder memory (deleted)", taskId);
    }
    
    // 更新内存中的任务信息
    private void UpdateTaskInMemory(VikunjaTask task, VikunjaProject project)
    {
        var labelIds = task.Labels?.Select(l => l.Id).ToList() ?? new List<long>();
        var reminders = task.Reminders?.Select(r => r.Reminder).ToList() ?? new List<DateTime>();
        
        var info = new TaskReminderInfo(
            task.Id,
            task.Title ?? string.Empty,
            project.Id,
            project.Title ?? string.Empty,
            task.StartDate,
            task.DueDate,
            task.EndDate,
            reminders,
            labelIds
        );
        
        _pendingReminders.AddOrUpdate(task.Id, info, (_, _) => info);
    }
    
    // 定时检查内存中的待提醒任务
    private async Task CheckPendingRemindersAsync()
    {
        try
        {
            var configs = await _configManager.LoadAllConfigsAsync(CancellationToken.None);
            var enabledConfigs = configs.Where(c => c.ReminderConfig?.Enabled == true).ToList();
            
            if (!enabledConfigs.Any())
            {
                return;
            }
            
            var now = DateTime.UtcNow;
            var checkWindow = TimeSpan.FromMinutes(5); // 检查未来5分钟内的提醒
            
            foreach (var kvp in _pendingReminders)
            {
                var taskInfo = kvp.Value;
                
                // 检查开始时间
                if (taskInfo.StartDate.HasValue && ShouldSendReminder(taskInfo.StartDate.Value, now, checkWindow))
                {
                    await ProcessReminderAsync(taskInfo, "start", taskInfo.StartDate.Value, enabledConfigs);
                }
                
                // 检查截止时间
                if (taskInfo.DueDate.HasValue && ShouldSendReminder(taskInfo.DueDate.Value, now, checkWindow))
                {
                    await ProcessReminderAsync(taskInfo, "due", taskInfo.DueDate.Value, enabledConfigs);
                }
                
                // 检查结束时间
                if (taskInfo.EndDate.HasValue && ShouldSendReminder(taskInfo.EndDate.Value, now, checkWindow))
                {
                    await ProcessReminderAsync(taskInfo, "end", taskInfo.EndDate.Value, enabledConfigs);
                }
                
                // 检查提醒时间
                foreach (var reminderTime in taskInfo.Reminders)
                {
                    if (ShouldSendReminder(reminderTime, now, checkWindow))
                    {
                        await ProcessReminderAsync(taskInfo, "reminder", reminderTime, enabledConfigs);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pending reminders");
        }
    }
    
    // 判断是否应该发送提醒
    private bool ShouldSendReminder(DateTime reminderTime, DateTime now, TimeSpan checkWindow)
    {
        var diff = reminderTime - now;
        return diff >= TimeSpan.Zero && diff <= checkWindow;
    }
    
    // 处理提醒发送
    private async Task ProcessReminderAsync(TaskReminderInfo taskInfo, string reminderType, DateTime reminderTime, List<UserConfig> configs)
    {
        var key = $"{taskInfo.TaskId}_{reminderType}_{reminderTime:yyyy-MM-dd HH:mm}";
        
        // 检查是否已发送
        if (_sentReminders.ContainsKey(key))
        {
            return;
        }
        
        // 标记为已发送
        _sentReminders.TryAdd(key, DateTime.UtcNow);
        
        // 发送提醒给所有启用的用户
        foreach (var config in configs)
        {
            if (ShouldSendReminderForUser(config, taskInfo))
            {
                await SendReminderAsync(config, taskInfo, reminderType);
            }
        }
        
        _logger.LogInformation("Sent {Type} reminder for task {TaskId} at {Time}", reminderType, taskInfo.TaskId, reminderTime);
    }
    
    // 判断是否应该为该用户发送提醒
    private bool ShouldSendReminderForUser(UserConfig config, TaskReminderInfo taskInfo)
    {
        if (config.ReminderConfig!.EnableLabelFilter && config.ReminderConfig.FilterLabelIds.Any())
        {
            return taskInfo.LabelIds.Any(labelId => config.ReminderConfig.FilterLabelIds.Contains(labelId));
        }
        
        return true;
    }
    
    // 清理已发送记录
    private void CleanupSentReminders()
    {
        try
        {
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var expiredKeys = _sentReminders
                .Where(kvp => kvp.Value < twoHoursAgo)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _sentReminders.TryRemove(key, out _);
            }
            
            // 防止内存泄漏
            if (_sentReminders.Count > MaxSentRemindersSize)
            {
                var toRemove = _sentReminders
                    .OrderBy(kvp => kvp.Value)
                    .Take(_sentReminders.Count - MaxSentRemindersSize)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in toRemove)
                {
                    _sentReminders.TryRemove(key, out _);
                }
                
                _logger.LogWarning("Sent reminders size exceeded {MaxSize}, removed {Count} oldest entries", 
                    MaxSentRemindersSize, toRemove.Count);
            }
            
            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired sent reminder records, current size: {Size}", 
                    expiredKeys.Count, _sentReminders.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up sent reminders");
        }
    }


    private async Task SendReminderAsync(UserConfig config, TaskReminderInfo taskInfo, string reminderType)
    {
        try
        {
            var reminderConfig = config.ReminderConfig!;
            
            // Select template based on reminder type
            var template = reminderType switch
            {
                "start" => reminderConfig.StartDateTemplate,
                "due" => reminderConfig.DueDateTemplate,
                "end" => reminderConfig.EndDateTemplate,
                "reminder" => reminderConfig.ReminderTimeTemplate,
                _ => reminderConfig.ReminderTimeTemplate
            };
            
            // Build context for template
            var context = new TemplateContext
            {
                Task = new TaskTemplateData
                {
                    Id = (int)taskInfo.TaskId,
                    Title = taskInfo.Title,
                    Description = string.Empty,
                    Done = false,
                    Priority = 0,
                    DueDate = taskInfo.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/tasks/{taskInfo.TaskId}" 
                        : $"Task ID: {taskInfo.TaskId}"
                },
                Project = new ProjectTemplateData
                {
                    Id = (int)taskInfo.ProjectId,
                    Title = taskInfo.ProjectTitle,
                    Description = string.Empty,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/projects/{taskInfo.ProjectId}" 
                        : $"Project ID: {taskInfo.ProjectId}"
                },
                Event = new EventData
                {
                    Type = $"task.reminder.{reminderType}",
                    Timestamp = DateTime.UtcNow,
                    Url = !string.IsNullOrWhiteSpace(_vikunjaUrl) 
                        ? $"{_vikunjaUrl.TrimEnd('/')}/tasks/{taskInfo.TaskId}" 
                        : $"Task ID: {taskInfo.TaskId}"
                }
            };
            
            // Add custom properties for reminder-specific data
            var startDate = taskInfo.StartDate?.ToString("yyyy-MM-dd HH:mm") ?? "None";
            var endDate = taskInfo.EndDate?.ToString("yyyy-MM-dd HH:mm") ?? "None";
            var reminders = taskInfo.Reminders.Any() 
                ? string.Join(", ", taskInfo.Reminders.Select(r => r.ToString("yyyy-MM-dd HH:mm")))
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
                        TaskId = taskInfo.TaskId,
                        TaskTitle = taskInfo.Title,
                        ProjectTitle = taskInfo.ProjectTitle,
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
                            config.UserId, providerType, taskInfo.TaskId);
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
                        TaskId = taskInfo.TaskId,
                        TaskTitle = taskInfo.Title,
                        ProjectTitle = taskInfo.ProjectTitle,
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
            _logger.LogError(ex, "Error sending reminder for task {TaskId}", taskInfo.TaskId);
        }
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _pendingReminders.Clear();
        _sentReminders.Clear();
    }
    
    // 获取提醒状态（用于监控和调试）
    public ReminderStatus GetReminderStatus()
    {
        return new ReminderStatus
        {
            PendingTasks = _pendingReminders.Count,
            SentReminders = _sentReminders.Count,
            IsInitialized = _isInitialized,
            Tasks = _pendingReminders.Values.Take(50).Select(t => new TaskReminderSummary
            {
                TaskId = t.TaskId,
                Title = t.Title,
                ProjectTitle = t.ProjectTitle,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                EndDate = t.EndDate,
                ReminderCount = t.Reminders.Count
            }).ToList()
        };
    }
}

// 提醒状态响应
public record ReminderStatus
{
    public int PendingTasks { get; init; }
    public int SentReminders { get; init; }
    public bool IsInitialized { get; init; }
    public List<TaskReminderSummary> Tasks { get; init; } = new();
}

public record TaskReminderSummary
{
    public long TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ProjectTitle { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int ReminderCount { get; init; }
}

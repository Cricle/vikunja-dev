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
    private readonly IEnumerable<NotificationProviderBase> _providers;
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
        IEnumerable<NotificationProviderBase> providers,
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
            _logger.LogDebug("Task reminder service already initialized");
            return;
        }
        
        try
        {
            _logger.LogInformation("🔄 Initializing task reminders - scanning all tasks...");
            
            var projects = await _clientFactory.GetAsync<List<VikunjaProject>>("projects", CancellationToken.None) ?? new();
            _logger.LogInformation("Found {Count} projects to scan", projects.Count);
            
            var totalTasks = 0;
            var tasksWithReminders = 0;
            
            foreach (var project in projects)
            {
                try
                {
                    var tasks = await _clientFactory.GetAsync<List<VikunjaTask>>(
                        $"projects/{project.Id}/tasks?per_page=100", 
                        CancellationToken.None) ?? new();
                    
                    var projectTaskCount = 0;
                    
                    foreach (var task in tasks)
                    {
                        if (!task.Done)
                        {
                            UpdateTaskInMemory(task, project);
                            totalTasks++;
                            projectTaskCount++;
                            
                            if (task.Reminders?.Any() == true || task.StartDate.HasValue || task.DueDate.HasValue || task.EndDate.HasValue)
                            {
                                tasksWithReminders++;
                            }
                        }
                    }
                    
                    if (projectTaskCount > 0)
                    {
                        _logger.LogDebug("Loaded {Count} pending tasks from project {ProjectId} ({ProjectTitle})", 
                            projectTaskCount, project.Id, project.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load tasks from project {ProjectId} ({ProjectTitle})", 
                        project.Id, project.Title);
                }
            }
            
            _isInitialized = true;
            _logger.LogInformation("✓ Task reminder initialization complete - loaded {TotalTasks} pending tasks ({WithReminders} with reminders/dates)", 
                totalTasks, tasksWithReminders);
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
            _logger.LogInformation("✓ Task {TaskId} ({Title}) added to reminder memory - Project: {ProjectTitle}, Reminders: {ReminderCount}, StartDate: {StartDate}, DueDate: {DueDate}, EndDate: {EndDate}", 
                task.Id, task.Title, project.Title, task.Reminders?.Count ?? 0, task.StartDate, task.DueDate, task.EndDate);
        }
        else
        {
            _logger.LogDebug("Task {TaskId} ({Title}) is already done, not adding to reminder memory", task.Id, task.Title);
        }
    }
    
    public void OnTaskUpdated(VikunjaTask task, VikunjaProject project)
    {
        if (task.Done)
        {
            // 任务完成，从内存中移除
            var removed = _pendingReminders.TryRemove(task.Id, out var oldInfo);
            if (removed)
            {
                _logger.LogInformation("✓ Task {TaskId} ({Title}) removed from reminder memory (completed) - Had {ReminderCount} reminders", 
                    task.Id, task.Title, oldInfo?.Reminders.Count ?? 0);
            }
            else
            {
                _logger.LogDebug("Task {TaskId} ({Title}) marked as done but was not in reminder memory", task.Id, task.Title);
            }
        }
        else
        {
            // 更新任务信息
            var wasInMemory = _pendingReminders.ContainsKey(task.Id);
            UpdateTaskInMemory(task, project);
            
            if (wasInMemory)
            {
                _logger.LogInformation("✓ Task {TaskId} ({Title}) updated in reminder memory - Project: {ProjectTitle}, Reminders: {ReminderCount}, StartDate: {StartDate}, DueDate: {DueDate}, EndDate: {EndDate}", 
                    task.Id, task.Title, project.Title, task.Reminders?.Count ?? 0, task.StartDate, task.DueDate, task.EndDate);
            }
            else
            {
                _logger.LogInformation("✓ Task {TaskId} ({Title}) added to reminder memory (was not tracked before) - Project: {ProjectTitle}, Reminders: {ReminderCount}", 
                    task.Id, task.Title, project.Title, task.Reminders?.Count ?? 0);
            }
        }
    }
    
    public void OnTaskDeleted(long taskId)
    {
        var removed = _pendingReminders.TryRemove(taskId, out var info);
        if (removed)
        {
            _logger.LogInformation("✓ Task {TaskId} ({Title}) removed from reminder memory (deleted) - Had {ReminderCount} reminders", 
                taskId, info?.Title ?? "Unknown", info?.Reminders.Count ?? 0);
        }
        else
        {
            _logger.LogDebug("Task {TaskId} deleted but was not in reminder memory", taskId);
        }
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
        
        var now = DateTime.UtcNow;
        var upcomingReminders = reminders.Where(r => r > now && r <= now.AddMinutes(5)).ToList();
        
        if (upcomingReminders.Any())
        {
            _logger.LogInformation("⏰ Task {TaskId} has {Count} upcoming reminders in next 5 minutes: {Times}", 
                task.Id, upcomingReminders.Count, string.Join(", ", upcomingReminders.Select(r => r.ToString("yyyy-MM-dd HH:mm:ss"))));
        }
        
        _logger.LogDebug("Updated task {TaskId} in memory: Reminders={ReminderCount}, StartDate={StartDate}, DueDate={DueDate}, EndDate={EndDate}, Labels={LabelCount}",
            task.Id, reminders.Count, task.StartDate, task.DueDate, task.EndDate, labelIds.Count);
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
                _logger.LogDebug("Skipping reminder check - no enabled reminder configs");
                return;
            }
            
            var now = DateTime.UtcNow;
            var checkWindow = TimeSpan.FromMinutes(5); // 检查未来5分钟内的提醒
            
            _logger.LogDebug("🔍 Checking {Count} pending tasks for reminders (window: {Window} minutes, users: {Users})", 
                _pendingReminders.Count, checkWindow.TotalMinutes, enabledConfigs.Count);
            
            var remindersToSend = 0;
            
            foreach (var kvp in _pendingReminders)
            {
                var taskInfo = kvp.Value;
                
                // 检查开始时间
                if (taskInfo.StartDate.HasValue && ShouldSendReminder(taskInfo.StartDate.Value, now, checkWindow))
                {
                    _logger.LogInformation("⏰ Triggering START reminder for task {TaskId} ({Title}) at {Time}", 
                        taskInfo.TaskId, taskInfo.Title, taskInfo.StartDate.Value);
                    await ProcessReminderAsync(taskInfo, "start", taskInfo.StartDate.Value, enabledConfigs);
                    remindersToSend++;
                }
                
                // 检查截止时间
                if (taskInfo.DueDate.HasValue && ShouldSendReminder(taskInfo.DueDate.Value, now, checkWindow))
                {
                    _logger.LogInformation("⏰ Triggering DUE reminder for task {TaskId} ({Title}) at {Time}", 
                        taskInfo.TaskId, taskInfo.Title, taskInfo.DueDate.Value);
                    await ProcessReminderAsync(taskInfo, "due", taskInfo.DueDate.Value, enabledConfigs);
                    remindersToSend++;
                }
                
                // 检查结束时间
                if (taskInfo.EndDate.HasValue && ShouldSendReminder(taskInfo.EndDate.Value, now, checkWindow))
                {
                    _logger.LogInformation("⏰ Triggering END reminder for task {TaskId} ({Title}) at {Time}", 
                        taskInfo.TaskId, taskInfo.Title, taskInfo.EndDate.Value);
                    await ProcessReminderAsync(taskInfo, "end", taskInfo.EndDate.Value, enabledConfigs);
                    remindersToSend++;
                }
                
                // 检查提醒时间
                foreach (var reminderTime in taskInfo.Reminders)
                {
                    if (ShouldSendReminder(reminderTime, now, checkWindow))
                    {
                        _logger.LogInformation("⏰ Triggering REMINDER for task {TaskId} ({Title}) at {Time}", 
                            taskInfo.TaskId, taskInfo.Title, reminderTime);
                        await ProcessReminderAsync(taskInfo, "reminder", reminderTime, enabledConfigs);
                        remindersToSend++;
                    }
                }
            }
            
            if (remindersToSend > 0)
            {
                _logger.LogInformation("✓ Processed {Count} reminders in this check cycle", remindersToSend);
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
            _logger.LogDebug("Skipping duplicate reminder: {Key}", key);
            return;
        }
        
        _logger.LogInformation("📤 Processing {Type} reminder for task {TaskId} ({Title}) at {Time}", 
            reminderType.ToUpper(), taskInfo.TaskId, taskInfo.Title, reminderTime);
        
        // 标记为已发送
        _sentReminders.TryAdd(key, DateTime.UtcNow);
        
        var sentCount = 0;
        
        // 发送提醒给所有启用的用户
        foreach (var config in configs)
        {
            if (ShouldSendReminderForUser(config, taskInfo))
            {
                await SendReminderAsync(config, taskInfo, reminderType);
                sentCount++;
            }
            else
            {
                _logger.LogDebug("Skipping reminder for user {UserId} - label filter not matched", config.UserId);
            }
        }
        
        _logger.LogInformation("✓ Sent {Type} reminder for task {TaskId} to {Count} user(s)", 
            reminderType.ToUpper(), taskInfo.TaskId, sentCount);
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
                _ => reminderConfig.ReminderTimeTemplate
            };
            
            // Build context for template
            var context = BuildTemplateContext(taskInfo, reminderType);
            
            // Render title and body with custom replacements
            var (title, body) = RenderReminderTemplate(template, context, taskInfo, reminderType);
            
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
            await SendToProvidersAsync(config, providerTypes, message, taskInfo, reminderType, title, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for task {TaskId}", taskInfo.TaskId);
        }
    }

    private TemplateContext BuildTemplateContext(TaskReminderInfo taskInfo, string reminderType)
    {
        return new TemplateContext
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
    }

    private (string title, string body) RenderReminderTemplate(
        ReminderTemplate template,
        TemplateContext context,
        TaskReminderInfo taskInfo,
        string reminderType)
    {
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

        return (title, body);
    }

    private async Task SendToProvidersAsync(
        UserConfig config,
        List<string> providerTypes,
        NotificationMessage message,
        TaskReminderInfo taskInfo,
        string reminderType,
        string title,
        string body)
    {
        foreach (var providerType in providerTypes)
        {
            var providerConfig = config.Providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));
            
            if (providerConfig == null) continue;
            
            var provider = _providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null) continue;
            
            try
            {
                var result = await NotificationHelper.SendNotificationAsync(
                    provider,
                    providerConfig,
                    message,
                    CancellationToken.None
                );
                
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
                        Format = config.ReminderConfig!.Format.ToString()
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
                AddReminderHistoryRecord(
                    taskInfo, reminderType, config.UserId, 
                    title, body, providerType, 
                    result.Success, result.ErrorMessage
                );
                
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
                AddReminderHistoryRecord(
                    taskInfo, reminderType, config.UserId,
                    title, body, providerType,
                    false, ex.Message
                );
            }
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

    private void AddReminderHistoryRecord(
        TaskReminderInfo taskInfo,
        string reminderType,
        string userId,
        string title,
        string body,
        string providerType,
        bool success,
        string? errorMessage)
    {
        _reminderHistory.AddRecord(new TaskReminderRecord
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            TaskId = taskInfo.TaskId,
            TaskTitle = taskInfo.Title,
            ProjectTitle = taskInfo.ProjectTitle,
            ReminderType = reminderType,
            UserId = userId,
            Title = title,
            Body = body,
            Providers = new List<string> { providerType },
            Success = success,
            ErrorMessage = errorMessage
        });
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

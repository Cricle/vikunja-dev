using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// 自定义Webhook处理器示例
/// 展示如何继承 WebhookHandlerBase 并实现自定义逻辑
/// </summary>
public class CustomWebhookHandlerExample : WebhookHandlerBase
{
    private readonly ILogger<CustomWebhookHandlerExample> _customLogger;
    // 可以注入其他服务
    // private readonly IEmailService _emailService;
    // private readonly INotificationService _notificationService;

    public CustomWebhookHandlerExample(
        ILogger<CustomWebhookHandlerExample> logger
        // IEmailService emailService,
        // INotificationService notificationService
    ) : base(logger)
    {
        _customLogger = logger;
        // _emailService = emailService;
        // _notificationService = notificationService;
    }

    /// <summary>
    /// 示例：当任务创建时发送通知
    /// </summary>
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        _customLogger.LogInformation("🎯 新任务创建: {Title} (ID: {TaskId})", task?.Title, task?.Id);

        // 自定义逻辑示例
        if (task?.Priority >= 4) // 高优先级任务
        {
            _customLogger.LogWarning("⚠️ 高优先级任务创建，需要立即关注！");
            // await _emailService.SendHighPriorityTaskAlert(task);
            // await _notificationService.SendPushNotification($"高优先级任务: {task.Title}");
        }

        // 调用基类方法（如果需要）
        await base.HandleTaskCreatedAsync(payload);
    }

    /// <summary>
    /// 示例：当任务更新时检查状态变化
    /// </summary>
    protected override async Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var oldTask = payload.Data?.OldTask;

        _customLogger.LogInformation("📝 任务更新: {Title}", task?.Title);

        // 检查任务是否完成
        if (oldTask?.Done == false && task?.Done == true)
        {
            _customLogger.LogInformation("✅ 任务已完成: {Title}", task.Title);
            // await _notificationService.SendTaskCompletionNotification(task);
        }

        // 检查截止日期变化
        if (oldTask?.DueDate != task?.DueDate)
        {
            _customLogger.LogInformation("📅 截止日期已更新: {OldDate} -> {NewDate}", 
                oldTask?.DueDate, task?.DueDate);
        }

        await base.HandleTaskUpdatedAsync(payload);
    }

    /// <summary>
    /// 示例：当任务被分配时通知相关人员
    /// </summary>
    protected override async Task HandleTaskAssigneeCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var assignee = payload.Data?.Assignee;

        _customLogger.LogInformation("👤 任务分配: {TaskTitle} -> {Username}", 
            task?.Title, assignee?.Username);

        // 发送邮件通知被分配人
        // await _emailService.SendTaskAssignmentEmail(assignee.Email, task);

        await base.HandleTaskAssigneeCreatedAsync(payload);
    }

    /// <summary>
    /// 示例：当添加评论时进行内容分析
    /// </summary>
    protected override async Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        var task = payload.Data?.Task;

        _customLogger.LogInformation("💬 新评论: 任务 {TaskId}", comment?.TaskId);

        // 检查评论中是否包含 @mention
        if (comment?.Comment?.Contains("@") == true)
        {
            _customLogger.LogInformation("📢 评论中包含提及，需要通知相关用户");
            // await ProcessMentions(comment.Comment, task);
        }

        await base.HandleTaskCommentCreatedAsync(payload);
    }

    /// <summary>
    /// 示例：当项目创建时初始化默认设置
    /// </summary>
    protected override async Task HandleProjectCreatedAsync(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;

        _customLogger.LogInformation("📁 新项目创建: {Title}", project?.Title);

        // 自动创建默认标签或任务模板
        // await CreateDefaultLabelsForProject(project.Id);
        // await CreateDefaultTasksForProject(project.Id);

        await base.HandleProjectCreatedAsync(payload);
    }

    /// <summary>
    /// 示例：当标签创建时同步到其他系统
    /// </summary>
    protected override async Task HandleLabelCreatedAsync(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;

        _customLogger.LogInformation("🏷️ 新标签创建: {Title} (颜色: {Color})", 
            label?.Title, label?.HexColor);

        // 同步到外部系统
        // await _externalSystemService.SyncLabel(label);

        await base.HandleLabelCreatedAsync(payload);
    }

    /// <summary>
    /// 示例：覆盖主处理方法以添加全局逻辑
    /// </summary>
    public override async Task HandleWebhookAsync(VikunjaWebhookPayload payload)
    {
        // 在所有事件处理前执行的逻辑
        _customLogger.LogInformation("🔔 收到Webhook事件: {EventName} at {Time}", 
            payload.EventName, payload.Time);

        // 记录到数据库或分析系统
        // await _analyticsService.TrackWebhookEvent(payload);

        // 调用基类的事件分发逻辑
        await base.HandleWebhookAsync(payload);

        // 在所有事件处理后执行的逻辑
        _customLogger.LogDebug("✅ Webhook事件处理完成: {EventName}", payload.EventName);
    }

    /// <summary>
    /// 示例：处理未知事件时记录详细信息
    /// </summary>
    protected override async Task HandleUnknownEventAsync(VikunjaWebhookPayload payload)
    {
        _customLogger.LogWarning("❓ 未知事件类型: {EventName}, 数据: {@Data}", 
            payload.EventName, payload.Data);

        // 可以将未知事件发送到监控系统
        // await _monitoringService.ReportUnknownEvent(payload);

        await base.HandleUnknownEventAsync(payload);
    }

    // 私有辅助方法示例
    // private async Task ProcessMentions(string comment, VikunjaTask task)
    // {
    //     var mentions = ExtractMentions(comment);
    //     foreach (var username in mentions)
    //     {
    //         await _notificationService.NotifyUser(username, task);
    //     }
    // }
    //
    // private List<string> ExtractMentions(string text)
    // {
    //     // 提取 @username 格式的提及
    //     var regex = new Regex(@"@(\w+)");
    //     return regex.Matches(text)
    //         .Select(m => m.Groups[1].Value)
    //         .ToList();
    // }
}

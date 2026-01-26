using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Webhook处理器基类，提供默认的事件分发逻辑
/// </summary>
public abstract class WebhookHandlerBase : IWebhookHandler
{
    protected readonly ILogger Logger;

    protected WebhookHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// 处理webhook事件的主入口
    /// </summary>
    public virtual Task HandleWebhookAsync(VikunjaWebhookPayload payload)
    {
        Logger.LogInformation("收到Vikunja webhook事件: {EventName} at {Time}", 
            payload.EventName, payload.Time);

        return payload.EventName switch
        {
            VikunjaEventTypes.TaskCreated => HandleTaskCreatedAsync(payload),
            VikunjaEventTypes.TaskUpdated => HandleTaskUpdatedAsync(payload),
            VikunjaEventTypes.TaskDeleted => HandleTaskDeletedAsync(payload),
            
            VikunjaEventTypes.ProjectCreated => HandleProjectCreatedAsync(payload),
            VikunjaEventTypes.ProjectUpdated => HandleProjectUpdatedAsync(payload),
            VikunjaEventTypes.ProjectDeleted => HandleProjectDeletedAsync(payload),
            
            VikunjaEventTypes.TaskAssigneeCreated => HandleTaskAssigneeCreatedAsync(payload),
            VikunjaEventTypes.TaskAssigneeDeleted => HandleTaskAssigneeDeletedAsync(payload),
            
            VikunjaEventTypes.TaskCommentCreated => HandleTaskCommentCreatedAsync(payload),
            VikunjaEventTypes.TaskCommentUpdated => HandleTaskCommentUpdatedAsync(payload),
            VikunjaEventTypes.TaskCommentDeleted => HandleTaskCommentDeletedAsync(payload),
            
            VikunjaEventTypes.TaskAttachmentCreated => HandleTaskAttachmentCreatedAsync(payload),
            VikunjaEventTypes.TaskAttachmentDeleted => HandleTaskAttachmentDeletedAsync(payload),
            
            VikunjaEventTypes.TaskRelationCreated => HandleTaskRelationCreatedAsync(payload),
            VikunjaEventTypes.TaskRelationDeleted => HandleTaskRelationDeletedAsync(payload),
            
            VikunjaEventTypes.LabelCreated => HandleLabelCreatedAsync(payload),
            VikunjaEventTypes.LabelUpdated => HandleLabelUpdatedAsync(payload),
            VikunjaEventTypes.LabelDeleted => HandleLabelDeletedAsync(payload),
            
            VikunjaEventTypes.TaskLabelCreated => HandleTaskLabelCreatedAsync(payload),
            VikunjaEventTypes.TaskLabelDeleted => HandleTaskLabelDeletedAsync(payload),
            
            VikunjaEventTypes.UserCreated => HandleUserCreatedAsync(payload),
            
            VikunjaEventTypes.TeamCreated => HandleTeamCreatedAsync(payload),
            VikunjaEventTypes.TeamUpdated => HandleTeamUpdatedAsync(payload),
            VikunjaEventTypes.TeamDeleted => HandleTeamDeletedAsync(payload),
            
            VikunjaEventTypes.TeamMemberAdded => HandleTeamMemberAddedAsync(payload),
            VikunjaEventTypes.TeamMemberRemoved => HandleTeamMemberRemovedAsync(payload),
            
            _ => HandleUnknownEventAsync(payload)
        };
    }

    // Task Events
    protected virtual Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Project Events
    protected virtual Task HandleProjectCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleProjectUpdatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleProjectDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Task Assignee Events
    protected virtual Task HandleTaskAssigneeCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskAssigneeDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Task Comment Events
    protected virtual Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskCommentUpdatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskCommentDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Task Attachment Events
    protected virtual Task HandleTaskAttachmentCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskAttachmentDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Task Relation Events
    protected virtual Task HandleTaskRelationCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskRelationDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Label Events
    protected virtual Task HandleLabelCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleLabelUpdatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleLabelDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Task Label Events
    protected virtual Task HandleTaskLabelCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTaskLabelDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // User Events
    protected virtual Task HandleUserCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Team Events
    protected virtual Task HandleTeamCreatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTeamUpdatedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTeamDeletedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Team Member Events
    protected virtual Task HandleTeamMemberAddedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleTeamMemberRemovedAsync(VikunjaWebhookPayload payload)
    {
        return Task.CompletedTask;
    }

    // Unknown Event
    protected virtual Task HandleUnknownEventAsync(VikunjaWebhookPayload payload)
    {
        Logger.LogWarning("未知的webhook事件类型: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }
}

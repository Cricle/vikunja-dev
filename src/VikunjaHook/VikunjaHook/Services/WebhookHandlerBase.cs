using Microsoft.Extensions.Logging;
using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Base class for webhook handlers with virtual methods for all event types.
/// Inherit from this class and override specific event methods you want to handle.
/// </summary>
public abstract class WebhookHandlerBase : IWebhookHandler
{
    protected readonly ILogger Logger;

    protected WebhookHandlerBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Main webhook handler that routes events to specific virtual methods
    /// </summary>
    public virtual async Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Processing webhook event: {EventName}", payload.EventName);

        try
        {
            await (payload.EventName switch
            {
                // Task events
                VikunjaEventTypes.TaskCreated => OnTaskCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskUpdated => OnTaskUpdatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskDeleted => OnTaskDeletedAsync(payload, cancellationToken),

                // Project events
                VikunjaEventTypes.ProjectCreated => OnProjectCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.ProjectUpdated => OnProjectUpdatedAsync(payload, cancellationToken),
                VikunjaEventTypes.ProjectDeleted => OnProjectDeletedAsync(payload, cancellationToken),

                // Task assignee events
                VikunjaEventTypes.TaskAssigneeCreated => OnTaskAssigneeCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskAssigneeDeleted => OnTaskAssigneeDeletedAsync(payload, cancellationToken),

                // Task comment events
                VikunjaEventTypes.TaskCommentCreated => OnTaskCommentCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskCommentUpdated => OnTaskCommentUpdatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskCommentDeleted => OnTaskCommentDeletedAsync(payload, cancellationToken),

                // Task attachment events
                VikunjaEventTypes.TaskAttachmentCreated => OnTaskAttachmentCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskAttachmentDeleted => OnTaskAttachmentDeletedAsync(payload, cancellationToken),

                // Task relation events
                VikunjaEventTypes.TaskRelationCreated => OnTaskRelationCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskRelationDeleted => OnTaskRelationDeletedAsync(payload, cancellationToken),

                // Label events
                VikunjaEventTypes.LabelCreated => OnLabelCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.LabelUpdated => OnLabelUpdatedAsync(payload, cancellationToken),
                VikunjaEventTypes.LabelDeleted => OnLabelDeletedAsync(payload, cancellationToken),

                // Task label events
                VikunjaEventTypes.TaskLabelCreated => OnTaskLabelCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TaskLabelDeleted => OnTaskLabelDeletedAsync(payload, cancellationToken),

                // User events
                VikunjaEventTypes.UserCreated => OnUserCreatedAsync(payload, cancellationToken),

                // Team events
                VikunjaEventTypes.TeamCreated => OnTeamCreatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TeamUpdated => OnTeamUpdatedAsync(payload, cancellationToken),
                VikunjaEventTypes.TeamDeleted => OnTeamDeletedAsync(payload, cancellationToken),

                // Team member events
                VikunjaEventTypes.TeamMemberAdded => OnTeamMemberAddedAsync(payload, cancellationToken),
                VikunjaEventTypes.TeamMemberRemoved => OnTeamMemberRemovedAsync(payload, cancellationToken),

                // Unknown event
                _ => OnUnknownEventAsync(payload, cancellationToken)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling webhook event: {EventName}", payload.EventName);
            await OnErrorAsync(payload, ex, cancellationToken);
            throw;
        }
    }

    // ===== Task Events =====

    /// <summary>
    /// Called when a task is created
    /// </summary>
    protected virtual Task OnTaskCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a task is updated
    /// </summary>
    protected virtual Task OnTaskUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task updated: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a task is deleted
    /// </summary>
    protected virtual Task OnTaskDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Project Events =====

    /// <summary>
    /// Called when a project is created
    /// </summary>
    protected virtual Task OnProjectCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Project created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a project is updated
    /// </summary>
    protected virtual Task OnProjectUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Project updated: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a project is deleted
    /// </summary>
    protected virtual Task OnProjectDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Project deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Task Assignee Events =====

    /// <summary>
    /// Called when an assignee is added to a task
    /// </summary>
    protected virtual Task OnTaskAssigneeCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task assignee created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an assignee is removed from a task
    /// </summary>
    protected virtual Task OnTaskAssigneeDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task assignee deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Task Comment Events =====

    /// <summary>
    /// Called when a comment is created on a task
    /// </summary>
    protected virtual Task OnTaskCommentCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task comment created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a comment is updated on a task
    /// </summary>
    protected virtual Task OnTaskCommentUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task comment updated: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a comment is deleted from a task
    /// </summary>
    protected virtual Task OnTaskCommentDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task comment deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Task Attachment Events =====

    /// <summary>
    /// Called when an attachment is added to a task
    /// </summary>
    protected virtual Task OnTaskAttachmentCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task attachment created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an attachment is deleted from a task
    /// </summary>
    protected virtual Task OnTaskAttachmentDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task attachment deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Task Relation Events =====

    /// <summary>
    /// Called when a relation is created between tasks
    /// </summary>
    protected virtual Task OnTaskRelationCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task relation created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a relation is deleted between tasks
    /// </summary>
    protected virtual Task OnTaskRelationDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task relation deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Label Events =====

    /// <summary>
    /// Called when a label is created
    /// </summary>
    protected virtual Task OnLabelCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Label created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a label is updated
    /// </summary>
    protected virtual Task OnLabelUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Label updated: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a label is deleted
    /// </summary>
    protected virtual Task OnLabelDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Label deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Task Label Events =====

    /// <summary>
    /// Called when a label is added to a task
    /// </summary>
    protected virtual Task OnTaskLabelCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task label created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a label is removed from a task
    /// </summary>
    protected virtual Task OnTaskLabelDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Task label deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== User Events =====

    /// <summary>
    /// Called when a user is created
    /// </summary>
    protected virtual Task OnUserCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("User created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Team Events =====

    /// <summary>
    /// Called when a team is created
    /// </summary>
    protected virtual Task OnTeamCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Team created: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a team is updated
    /// </summary>
    protected virtual Task OnTeamUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Team updated: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a team is deleted
    /// </summary>
    protected virtual Task OnTeamDeletedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Team deleted: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Team Member Events =====

    /// <summary>
    /// Called when a member is added to a team
    /// </summary>
    protected virtual Task OnTeamMemberAddedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Team member added: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a member is removed from a team
    /// </summary>
    protected virtual Task OnTeamMemberRemovedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Team member removed: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    // ===== Special Events =====

    /// <summary>
    /// Called when an unknown event is received
    /// </summary>
    protected virtual Task OnUnknownEventAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogWarning("Unknown event received: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an error occurs during event processing
    /// </summary>
    protected virtual Task OnErrorAsync(VikunjaWebhookPayload payload, Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(exception, "Error processing event: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }
}

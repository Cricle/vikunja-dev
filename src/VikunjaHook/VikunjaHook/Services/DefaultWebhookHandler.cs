using VikunjaHook.Models;

namespace VikunjaHook.Services;

public class DefaultWebhookHandler : IWebhookHandler
{
    private readonly ILogger<DefaultWebhookHandler> _logger;

    public DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleWebhookAsync(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("收到Vikunja webhook事件: {EventName} at {Time}", 
            payload.EventName, payload.Time);

        return payload.EventName switch
        {
            VikunjaEventTypes.TaskCreated => HandleTaskCreated(payload),
            VikunjaEventTypes.TaskUpdated => HandleTaskUpdated(payload),
            VikunjaEventTypes.TaskDeleted => HandleTaskDeleted(payload),
            
            VikunjaEventTypes.ProjectCreated => HandleProjectCreated(payload),
            VikunjaEventTypes.ProjectUpdated => HandleProjectUpdated(payload),
            VikunjaEventTypes.ProjectDeleted => HandleProjectDeleted(payload),
            
            VikunjaEventTypes.TaskAssigneeCreated => HandleTaskAssigneeCreated(payload),
            VikunjaEventTypes.TaskAssigneeDeleted => HandleTaskAssigneeDeleted(payload),
            
            VikunjaEventTypes.TaskCommentCreated => HandleTaskCommentCreated(payload),
            VikunjaEventTypes.TaskCommentUpdated => HandleTaskCommentUpdated(payload),
            VikunjaEventTypes.TaskCommentDeleted => HandleTaskCommentDeleted(payload),
            
            VikunjaEventTypes.TaskAttachmentCreated => HandleTaskAttachmentCreated(payload),
            VikunjaEventTypes.TaskAttachmentDeleted => HandleTaskAttachmentDeleted(payload),
            
            VikunjaEventTypes.TaskRelationCreated => HandleTaskRelationCreated(payload),
            VikunjaEventTypes.TaskRelationDeleted => HandleTaskRelationDeleted(payload),
            
            VikunjaEventTypes.LabelCreated => HandleLabelCreated(payload),
            VikunjaEventTypes.LabelUpdated => HandleLabelUpdated(payload),
            VikunjaEventTypes.LabelDeleted => HandleLabelDeleted(payload),
            
            VikunjaEventTypes.TaskLabelCreated => HandleTaskLabelCreated(payload),
            VikunjaEventTypes.TaskLabelDeleted => HandleTaskLabelDeleted(payload),
            
            VikunjaEventTypes.UserCreated => HandleUserCreated(payload),
            
            VikunjaEventTypes.TeamCreated => HandleTeamCreated(payload),
            VikunjaEventTypes.TeamUpdated => HandleTeamUpdated(payload),
            VikunjaEventTypes.TeamDeleted => HandleTeamDeleted(payload),
            
            VikunjaEventTypes.TeamMemberAdded => HandleTeamMemberAdded(payload),
            VikunjaEventTypes.TeamMemberRemoved => HandleTeamMemberRemoved(payload),
            
            _ => HandleUnknownEvent(payload)
        };
    }

    private Task HandleTaskCreated(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        _logger.LogInformation("任务创建: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    private Task HandleTaskUpdated(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var oldTask = payload.Data?.OldTask;
        _logger.LogInformation("任务更新: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    private Task HandleTaskDeleted(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        _logger.LogInformation("任务删除: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    private Task HandleProjectCreated(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        _logger.LogInformation("项目创建: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    private Task HandleProjectUpdated(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        _logger.LogInformation("项目更新: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    private Task HandleProjectDeleted(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        _logger.LogInformation("项目删除: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    private Task HandleTaskAssigneeCreated(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var assignee = payload.Data?.Assignee;
        _logger.LogInformation("任务分配: 任务ID={TaskId}, 分配给={Username}", 
            task?.Id, assignee?.Username);
        return Task.CompletedTask;
    }

    private Task HandleTaskAssigneeDeleted(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var assignee = payload.Data?.Assignee;
        _logger.LogInformation("取消任务分配: 任务ID={TaskId}, 用户={Username}", 
            task?.Id, assignee?.Username);
        return Task.CompletedTask;
    }

    private Task HandleTaskCommentCreated(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        _logger.LogInformation("评论创建: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    private Task HandleTaskCommentUpdated(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        _logger.LogInformation("评论更新: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    private Task HandleTaskCommentDeleted(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        _logger.LogInformation("评论删除: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    private Task HandleTaskAttachmentCreated(VikunjaWebhookPayload payload)
    {
        var attachment = payload.Data?.Attachment;
        _logger.LogInformation("附件创建: ID={AttachmentId}, 任务ID={TaskId}", 
            attachment?.Id, attachment?.TaskId);
        return Task.CompletedTask;
    }

    private Task HandleTaskAttachmentDeleted(VikunjaWebhookPayload payload)
    {
        var attachment = payload.Data?.Attachment;
        _logger.LogInformation("附件删除: ID={AttachmentId}, 任务ID={TaskId}", 
            attachment?.Id, attachment?.TaskId);
        return Task.CompletedTask;
    }

    private Task HandleTaskRelationCreated(VikunjaWebhookPayload payload)
    {
        var relation = payload.Data?.Relation;
        _logger.LogInformation("任务关系创建: 任务ID={TaskId}, 关联任务ID={OtherTaskId}, 类型={Kind}", 
            relation?.TaskId, relation?.OtherTaskId, relation?.RelationKind);
        return Task.CompletedTask;
    }

    private Task HandleTaskRelationDeleted(VikunjaWebhookPayload payload)
    {
        var relation = payload.Data?.Relation;
        _logger.LogInformation("任务关系删除: 任务ID={TaskId}, 关联任务ID={OtherTaskId}", 
            relation?.TaskId, relation?.OtherTaskId);
        return Task.CompletedTask;
    }

    private Task HandleLabelCreated(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        _logger.LogInformation("标签创建: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    private Task HandleLabelUpdated(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        _logger.LogInformation("标签更新: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    private Task HandleLabelDeleted(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        _logger.LogInformation("标签删除: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    private Task HandleTaskLabelCreated(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var label = payload.Data?.Label;
        _logger.LogInformation("任务标签添加: 任务ID={TaskId}, 标签={Label}", 
            task?.Id, label?.Title);
        return Task.CompletedTask;
    }

    private Task HandleTaskLabelDeleted(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var label = payload.Data?.Label;
        _logger.LogInformation("任务标签移除: 任务ID={TaskId}, 标签={Label}", 
            task?.Id, label?.Title);
        return Task.CompletedTask;
    }

    private Task HandleUserCreated(VikunjaWebhookPayload payload)
    {
        var user = payload.Data?.User;
        _logger.LogInformation("用户创建: ID={UserId}, 用户名={Username}", 
            user?.Id, user?.Username);
        return Task.CompletedTask;
    }

    private Task HandleTeamCreated(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        _logger.LogInformation("团队创建: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    private Task HandleTeamUpdated(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        _logger.LogInformation("团队更新: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    private Task HandleTeamDeleted(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        _logger.LogInformation("团队删除: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    private Task HandleTeamMemberAdded(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        var member = payload.Data?.Member;
        _logger.LogInformation("团队成员添加: 团队ID={TeamId}, 成员={Username}", 
            team?.Id, member?.Username);
        return Task.CompletedTask;
    }

    private Task HandleTeamMemberRemoved(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        var member = payload.Data?.Member;
        _logger.LogInformation("团队成员移除: 团队ID={TeamId}, 成员={Username}", 
            team?.Id, member?.Username);
        return Task.CompletedTask;
    }

    private Task HandleUnknownEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogWarning("未知的webhook事件类型: {EventName}", payload.EventName);
        return Task.CompletedTask;
    }
}

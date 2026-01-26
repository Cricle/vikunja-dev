using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// 默认的Webhook处理器实现，提供基本的日志记录功能
/// </summary>
public class DefaultWebhookHandler : WebhookHandlerBase
{
    public DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger) : base(logger)
    {
    }

    protected override Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        Logger.LogInformation("任务创建: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var oldTask = payload.Data?.OldTask;
        Logger.LogInformation("任务更新: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskDeletedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        Logger.LogInformation("任务删除: ID={TaskId}, 标题={Title}", task?.Id, task?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleProjectCreatedAsync(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        Logger.LogInformation("项目创建: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleProjectUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        Logger.LogInformation("项目更新: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleProjectDeletedAsync(VikunjaWebhookPayload payload)
    {
        var project = payload.Data?.Project;
        Logger.LogInformation("项目删除: ID={ProjectId}, 标题={Title}", project?.Id, project?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskAssigneeCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var assignee = payload.Data?.Assignee;
        Logger.LogInformation("任务分配: 任务ID={TaskId}, 分配给={Username}", 
            task?.Id, assignee?.Username);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskAssigneeDeletedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var assignee = payload.Data?.Assignee;
        Logger.LogInformation("取消任务分配: 任务ID={TaskId}, 用户={Username}", 
            task?.Id, assignee?.Username);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        Logger.LogInformation("评论创建: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskCommentUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        Logger.LogInformation("评论更新: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskCommentDeletedAsync(VikunjaWebhookPayload payload)
    {
        var comment = payload.Data?.Comment;
        Logger.LogInformation("评论删除: ID={CommentId}, 任务ID={TaskId}", 
            comment?.Id, comment?.TaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskAttachmentCreatedAsync(VikunjaWebhookPayload payload)
    {
        var attachment = payload.Data?.Attachment;
        Logger.LogInformation("附件创建: ID={AttachmentId}, 任务ID={TaskId}", 
            attachment?.Id, attachment?.TaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskAttachmentDeletedAsync(VikunjaWebhookPayload payload)
    {
        var attachment = payload.Data?.Attachment;
        Logger.LogInformation("附件删除: ID={AttachmentId}, 任务ID={TaskId}", 
            attachment?.Id, attachment?.TaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskRelationCreatedAsync(VikunjaWebhookPayload payload)
    {
        var relation = payload.Data?.Relation;
        Logger.LogInformation("任务关系创建: 任务ID={TaskId}, 关联任务ID={OtherTaskId}, 类型={Kind}", 
            relation?.TaskId, relation?.OtherTaskId, relation?.RelationKind);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskRelationDeletedAsync(VikunjaWebhookPayload payload)
    {
        var relation = payload.Data?.Relation;
        Logger.LogInformation("任务关系删除: 任务ID={TaskId}, 关联任务ID={OtherTaskId}", 
            relation?.TaskId, relation?.OtherTaskId);
        return Task.CompletedTask;
    }

    protected override Task HandleLabelCreatedAsync(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        Logger.LogInformation("标签创建: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleLabelUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        Logger.LogInformation("标签更新: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleLabelDeletedAsync(VikunjaWebhookPayload payload)
    {
        var label = payload.Data?.Label;
        Logger.LogInformation("标签删除: ID={LabelId}, 标题={Title}", label?.Id, label?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskLabelCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var label = payload.Data?.Label;
        Logger.LogInformation("任务标签添加: 任务ID={TaskId}, 标签={Label}", 
            task?.Id, label?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleTaskLabelDeletedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        var label = payload.Data?.Label;
        Logger.LogInformation("任务标签移除: 任务ID={TaskId}, 标签={Label}", 
            task?.Id, label?.Title);
        return Task.CompletedTask;
    }

    protected override Task HandleUserCreatedAsync(VikunjaWebhookPayload payload)
    {
        var user = payload.Data?.User;
        Logger.LogInformation("用户创建: ID={UserId}, 用户名={Username}", 
            user?.Id, user?.Username);
        return Task.CompletedTask;
    }

    protected override Task HandleTeamCreatedAsync(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        Logger.LogInformation("团队创建: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    protected override Task HandleTeamUpdatedAsync(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        Logger.LogInformation("团队更新: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    protected override Task HandleTeamDeletedAsync(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        Logger.LogInformation("团队删除: ID={TeamId}, 名称={Name}", team?.Id, team?.Name);
        return Task.CompletedTask;
    }

    protected override Task HandleTeamMemberAddedAsync(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        var member = payload.Data?.Member;
        Logger.LogInformation("团队成员添加: 团队ID={TeamId}, 成员={Username}", 
            team?.Id, member?.Username);
        return Task.CompletedTask;
    }

    protected override Task HandleTeamMemberRemovedAsync(VikunjaWebhookPayload payload)
    {
        var team = payload.Data?.Team;
        var member = payload.Data?.Member;
        Logger.LogInformation("团队成员移除: 团队ID={TeamId}, 成员={Username}", 
            team?.Id, member?.Username);
        return Task.CompletedTask;
    }
}

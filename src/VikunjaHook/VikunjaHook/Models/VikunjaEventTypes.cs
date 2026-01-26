namespace VikunjaHook.Models;

public static class VikunjaEventTypes
{
    // Task events
    public const string TaskCreated = "task.created";
    public const string TaskUpdated = "task.updated";
    public const string TaskDeleted = "task.deleted";

    // Project events
    public const string ProjectCreated = "project.created";
    public const string ProjectUpdated = "project.updated";
    public const string ProjectDeleted = "project.deleted";

    // Task assignee events
    public const string TaskAssigneeCreated = "task.assignee.created";
    public const string TaskAssigneeDeleted = "task.assignee.deleted";

    // Task comment events
    public const string TaskCommentCreated = "task.comment.created";
    public const string TaskCommentUpdated = "task.comment.updated";
    public const string TaskCommentDeleted = "task.comment.deleted";

    // Task attachment events
    public const string TaskAttachmentCreated = "task.attachment.created";
    public const string TaskAttachmentDeleted = "task.attachment.deleted";

    // Task relation events
    public const string TaskRelationCreated = "task.relation.created";
    public const string TaskRelationDeleted = "task.relation.deleted";

    // Label events
    public const string LabelCreated = "label.created";
    public const string LabelUpdated = "label.updated";
    public const string LabelDeleted = "label.deleted";

    // Task label events
    public const string TaskLabelCreated = "task.label.created";
    public const string TaskLabelDeleted = "task.label.deleted";

    // User events
    public const string UserCreated = "user.created";

    // Team events
    public const string TeamCreated = "team.created";
    public const string TeamUpdated = "team.updated";
    public const string TeamDeleted = "team.deleted";

    // Team member events
    public const string TeamMemberAdded = "team.member.added";
    public const string TeamMemberRemoved = "team.member.removed";

    public static bool IsValidEvent(string eventName)
    {
        return eventName switch
        {
            TaskCreated or TaskUpdated or TaskDeleted or
            ProjectCreated or ProjectUpdated or ProjectDeleted or
            TaskAssigneeCreated or TaskAssigneeDeleted or
            TaskCommentCreated or TaskCommentUpdated or TaskCommentDeleted or
            TaskAttachmentCreated or TaskAttachmentDeleted or
            TaskRelationCreated or TaskRelationDeleted or
            LabelCreated or LabelUpdated or LabelDeleted or
            TaskLabelCreated or TaskLabelDeleted or
            UserCreated or
            TeamCreated or TeamUpdated or TeamDeleted or
            TeamMemberAdded or TeamMemberRemoved => true,
            _ => false
        };
    }
}

namespace Vikunja.Core.Notifications.Models;

public static class EventTypes
{
    public const string TaskCreated = "task.created";
    public const string TaskUpdated = "task.updated";
    public const string TaskDeleted = "task.deleted";
    public const string TaskAssigned = "task.assigned";
    public const string TaskCommentCreated = "task.comment.created";
    public const string TaskCommentUpdated = "task.comment.updated";
    public const string TaskCommentDeleted = "task.comment.deleted";
    public const string TaskAttachmentCreated = "task.attachment.created";
    public const string TaskAttachmentDeleted = "task.attachment.deleted";
    public const string TaskRelationCreated = "task.relation.created";
    public const string TaskRelationDeleted = "task.relation.deleted";
    public const string ProjectCreated = "project.created";
    public const string ProjectUpdated = "project.updated";
    public const string ProjectDeleted = "project.deleted";
    public const string TeamMemberAdded = "team.member.added";
    public const string TeamMemberRemoved = "team.member.removed";
    
    public static readonly IReadOnlyList<string> All = new[]
    {
        TaskCreated, TaskUpdated, TaskDeleted, TaskAssigned,
        TaskCommentCreated, TaskCommentUpdated, TaskCommentDeleted,
        TaskAttachmentCreated, TaskAttachmentDeleted,
        TaskRelationCreated, TaskRelationDeleted,
        ProjectCreated, ProjectUpdated, ProjectDeleted,
        TeamMemberAdded, TeamMemberRemoved
    };
}

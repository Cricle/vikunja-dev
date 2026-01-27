namespace Vikunja.Core.Notifications.Models;

public class WebhookEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int ProjectId { get; set; }
    public TaskEventData? Task { get; set; }
    public ProjectEventData? Project { get; set; }
    public CommentEventData? Comment { get; set; }
    public AttachmentEventData? Attachment { get; set; }
    public RelationEventData? Relation { get; set; }
    public TeamEventData? Team { get; set; }
}

public class TaskEventData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Done { get; set; }
    public int ProjectId { get; set; }
}

public class ProjectEventData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CommentEventData
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TaskId { get; set; }
}

public class AttachmentEventData
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TaskId { get; set; }
}

public class RelationEventData
{
    public int TaskId { get; set; }
    public int RelatedTaskId { get; set; }
    public string RelationType { get; set; } = string.Empty;
}

public class TeamEventData
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

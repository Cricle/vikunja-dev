namespace Vikunja.Core.Notifications.Models;

public record TemplateContext
{
    public TaskTemplateData? Task { get; init; }
    public ProjectTemplateData? Project { get; init; }
    public UserTemplateData? User { get; init; }
    public TeamTemplateData? Team { get; init; }
    public LabelTemplateData? Label { get; init; }
    public CommentTemplateData? Comment { get; init; }
    public AttachmentTemplateData? Attachment { get; init; }
    public RelationTemplateData? Relation { get; init; }
    public EventData Event { get; init; } = new();
    public IReadOnlyList<string>? Assignees { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
}

public class TaskTemplateData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public bool Done { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class ProjectTemplateData
{
    public string Title { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class UserTemplateData
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Id { get; set; }
}

public class TeamTemplateData
{
    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class LabelTemplateData
{
    public string Title { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CommentTemplateData
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}

public class AttachmentTemplateData
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class RelationTemplateData
{
    public int TaskId { get; set; }
    public int RelatedTaskId { get; set; }
    public string RelationType { get; set; } = string.Empty;
}

public class EventData
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
}

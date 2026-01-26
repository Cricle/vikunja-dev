using System.Text.Json.Serialization;

namespace VikunjaHook.Models;

public class VikunjaWebhookPayload
{
    [JsonPropertyName("event_name")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("data")]
    public WebhookData? Data { get; set; }
}

public class WebhookData
{
    // Task related
    [JsonPropertyName("task")]
    public TaskData? Task { get; set; }

    [JsonPropertyName("old_task")]
    public TaskData? OldTask { get; set; }

    // Project related
    [JsonPropertyName("project")]
    public ProjectData? Project { get; set; }

    [JsonPropertyName("old_project")]
    public ProjectData? OldProject { get; set; }

    // Comment related
    [JsonPropertyName("comment")]
    public CommentData? Comment { get; set; }

    // Assignee related
    [JsonPropertyName("assignee")]
    public UserData? Assignee { get; set; }

    // Label related
    [JsonPropertyName("label")]
    public LabelData? Label { get; set; }

    // Relation related
    [JsonPropertyName("relation")]
    public RelationData? Relation { get; set; }

    // Attachment related
    [JsonPropertyName("attachment")]
    public AttachmentData? Attachment { get; set; }

    // User related
    [JsonPropertyName("user")]
    public UserData? User { get; set; }

    // Team related
    [JsonPropertyName("team")]
    public TeamData? Team { get; set; }

    // Member related
    [JsonPropertyName("member")]
    public UserData? Member { get; set; }
}

public class TaskData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("done_at")]
    public DateTime? DoneAt { get; set; }

    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("percent_done")]
    public double PercentDone { get; set; }

    [JsonPropertyName("project_id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("created_by")]
    public UserData? CreatedBy { get; set; }

    [JsonPropertyName("assignees")]
    public List<UserData>? Assignees { get; set; }

    [JsonPropertyName("labels")]
    public List<LabelData>? Labels { get; set; }
}

public class ProjectData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("owner")]
    public UserData? Owner { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("hex_color")]
    public string? HexColor { get; set; }
}

public class UserData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class CommentData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public UserData? Author { get; set; }

    [JsonPropertyName("task_id")]
    public long TaskId { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class LabelData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hex_color")]
    public string HexColor { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class RelationData
{
    [JsonPropertyName("task_id")]
    public long TaskId { get; set; }

    [JsonPropertyName("other_task_id")]
    public long OtherTaskId { get; set; }

    [JsonPropertyName("relation_kind")]
    public string RelationKind { get; set; } = string.Empty;

    [JsonPropertyName("created_by")]
    public UserData? CreatedBy { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }
}

public class AttachmentData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("task_id")]
    public long TaskId { get; set; }

    [JsonPropertyName("file_id")]
    public long FileId { get; set; }

    [JsonPropertyName("created_by")]
    public UserData? CreatedBy { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }
}

public class TeamData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("created_by")]
    public UserData? CreatedBy { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vikunja.Core.Notifications.Models;

public class WebhookEvent
{
    [JsonPropertyName("event_name")]
    public string EventName { get; set; } = string.Empty;
    
    [JsonPropertyName("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
    
    // Computed properties for easier access
    [JsonIgnore]
    public string EventType => EventName;
    
    [JsonIgnore]
    public DateTime Timestamp => Time;
    
    [JsonIgnore]
    public int ProjectId
    {
        get
        {
            if (Data.ValueKind == JsonValueKind.Object)
            {
                // 首先尝试从 data 根级别获取 project_id
                if (Data.TryGetProperty("project_id", out var projectId))
                    return projectId.GetInt32();
                if (Data.TryGetProperty("ProjectId", out var projectIdAlt))
                    return projectIdAlt.GetInt32();
                
                // 如果根级别没有，尝试从 task 对象中获取
                if (Data.TryGetProperty("task", out var taskElement) && 
                    taskElement.ValueKind == JsonValueKind.Object)
                {
                    if (taskElement.TryGetProperty("project_id", out var taskProjectId))
                        return taskProjectId.GetInt32();
                    if (taskElement.TryGetProperty("ProjectId", out var taskProjectIdAlt))
                        return taskProjectIdAlt.GetInt32();
                }
                
                // 最后尝试从 project 对象中获取
                if (Data.TryGetProperty("project", out var projectElement) && 
                    projectElement.ValueKind == JsonValueKind.Object)
                {
                    if (projectElement.TryGetProperty("id", out var projectIdFromProject))
                        return projectIdFromProject.GetInt32();
                }
            }
            return 0;
        }
    }
    
    [JsonIgnore]
    public TaskEventData? Task
    {
        get
        {
            if (Data.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    // Vikunja sends data as: { "task": {...}, "project": {...}, "doer": {...} }
                    // We need to extract the "task" property first
                    if (Data.TryGetProperty("task", out var taskElement))
                    {
                        return JsonSerializer.Deserialize(taskElement.GetRawText(), 
                            Vikunja.Core.Notifications.WebhookNotificationJsonContext.Default.TaskEventData);
                    }
                    
                    // Fallback: try to deserialize the whole data as task (for backward compatibility)
                    return JsonSerializer.Deserialize(Data.GetRawText(), 
                        Vikunja.Core.Notifications.WebhookNotificationJsonContext.Default.TaskEventData);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}

public class TaskEventData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("done")]
    public bool Done { get; set; }
    
    [JsonPropertyName("project_id")]
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

namespace Vikunja.Core.Notifications.Models;

public class TemplateContext
{
    public TaskData? Task { get; init; }
    public ProjectData? Project { get; init; }
    public UserData? User { get; init; }
    public EventData Event { get; init; } = new();
    public IReadOnlyList<string>? Assignees { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
}

public class TaskData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public bool Done { get; set; }
}

public class ProjectData
{
    public string Title { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UserData
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class EventData
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
}

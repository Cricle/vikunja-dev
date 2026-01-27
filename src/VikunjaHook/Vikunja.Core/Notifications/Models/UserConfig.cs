namespace Vikunja.Core.Notifications.Models;

public class UserConfig
{
    public string UserId { get; set; } = string.Empty;
    public List<ProviderConfig> Providers { get; set; } = new();
    public List<ProjectRule> ProjectRules { get; set; } = new();
    public Dictionary<string, NotificationTemplate> Templates { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class ProjectRule
{
    public string ProjectId { get; set; } = "*"; // "*" for all projects
    public List<string> EnabledEvents { get; set; } = new();
    public string? ProviderType { get; set; } // null = use all providers
}

public class NotificationTemplate
{
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationFormat Format { get; set; } = NotificationFormat.Text;
}

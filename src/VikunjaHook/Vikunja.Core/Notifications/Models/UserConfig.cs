namespace Vikunja.Core.Notifications.Models;

public class UserConfig
{
    public string UserId { get; set; } = string.Empty;
    public List<ProviderConfig> Providers { get; set; } = new();
    public Dictionary<string, NotificationTemplate> Templates { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class NotificationTemplate
{
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationFormat Format { get; set; } = NotificationFormat.Text;
}

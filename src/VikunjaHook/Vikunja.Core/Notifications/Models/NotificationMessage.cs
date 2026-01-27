namespace Vikunja.Core.Notifications.Models;

public record NotificationMessage(
    string Title,
    string Body,
    NotificationFormat Format = NotificationFormat.Text);

public enum NotificationFormat
{
    Text,
    Markdown,
    Html
}

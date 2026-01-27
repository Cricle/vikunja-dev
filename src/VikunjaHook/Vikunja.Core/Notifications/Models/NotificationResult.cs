namespace Vikunja.Core.Notifications.Models;

public record NotificationResult(
    bool Success,
    string? ErrorMessage = null,
    DateTime Timestamp = default)
{
    public NotificationResult() : this(false, null, DateTime.UtcNow)
    {
    }
}

namespace Vikunja.Core.Notifications.Models;

public record TestNotificationRequest(
    string ProviderType,
    string? Title,
    string? Body);

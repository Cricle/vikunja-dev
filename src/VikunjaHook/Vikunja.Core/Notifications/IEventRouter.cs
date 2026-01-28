using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public interface IEventRouter
{
    Task RouteEventAsync(
        WebhookEvent webhookEvent, 
        CancellationToken cancellationToken = default);
}

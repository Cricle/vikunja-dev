using Microsoft.Extensions.Logging;
using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Default webhook handler that logs all events.
/// This is a simple implementation that inherits from WebhookHandlerBase.
/// </summary>
public class DefaultWebhookHandler : WebhookHandlerBase
{
    public DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger) : base(logger)
    {
    }

    public override Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Webhook received: Event={EventName}, Time={Time}",
            payload.EventName,
            payload.Time);

        // Call base implementation which routes to specific virtual methods
        return base.HandleWebhookAsync(payload, cancellationToken);
    }
}

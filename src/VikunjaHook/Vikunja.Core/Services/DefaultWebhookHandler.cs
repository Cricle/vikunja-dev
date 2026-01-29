using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Models;
using Vikunja.Core.Notifications;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Services;

public class DefaultWebhookHandler : WebhookHandlerBase
{
    private readonly EventRouter _eventRouter;

    public DefaultWebhookHandler(
        ILogger<DefaultWebhookHandler> logger,
        EventRouter eventRouter) : base(logger)
    {
        _eventRouter = eventRouter;
    }

    public override async Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Received webhook event: {EventName}", payload.EventName);

        try
        {
            var webhookEvent = ConvertToWebhookEvent(payload);
            await _eventRouter.RouteEventAsync(webhookEvent, cancellationToken);
            await base.HandleWebhookAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling webhook event: {EventName}", payload.EventName);
            throw;
        }
    }

    private WebhookEvent ConvertToWebhookEvent(VikunjaWebhookPayload payload)
    {
        var dataJson = JsonSerializer.Serialize(payload.Data, AppJsonSerializerContext.Default.Object);
        var dataElement = JsonDocument.Parse(dataJson).RootElement;

        return new WebhookEvent
        {
            EventName = payload.EventName,
            Time = payload.Time,
            Data = dataElement
        };
    }
}

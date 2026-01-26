using Microsoft.Extensions.Logging;
using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Default webhook handler that logs events
/// </summary>
public class DefaultWebhookHandler : IWebhookHandler
{
    private readonly ILogger<DefaultWebhookHandler> _logger;

    public DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Webhook received: Event={EventName}, Time={Time}",
            payload.EventName,
            payload.Time);

        // Log event details based on type
        switch (payload.EventName)
        {
            case var e when e.StartsWith("task."):
                LogTaskEvent(payload);
                break;
            case var e when e.StartsWith("project."):
                LogProjectEvent(payload);
                break;
            case var e when e.StartsWith("label."):
                LogLabelEvent(payload);
                break;
            case var e when e.StartsWith("team."):
                LogTeamEvent(payload);
                break;
            case var e when e.StartsWith("user."):
                LogUserEvent(payload);
                break;
            default:
                _logger.LogInformation("Unknown event type: {EventName}", payload.EventName);
                break;
        }

        return Task.CompletedTask;
    }

    private void LogTaskEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("Task event: {EventName}", payload.EventName);
    }

    private void LogProjectEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("Project event: {EventName}", payload.EventName);
    }

    private void LogLabelEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("Label event: {EventName}", payload.EventName);
    }

    private void LogTeamEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("Team event: {EventName}", payload.EventName);
    }

    private void LogUserEvent(VikunjaWebhookPayload payload)
    {
        _logger.LogInformation("User event: {EventName}", payload.EventName);
    }
}

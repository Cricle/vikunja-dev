using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Interface for handling Vikunja webhook events
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Handle incoming webhook payload
    /// </summary>
    Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default);
}

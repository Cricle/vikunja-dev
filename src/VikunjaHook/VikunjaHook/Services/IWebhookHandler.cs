using VikunjaHook.Models;

namespace VikunjaHook.Services;

public interface IWebhookHandler
{
    Task HandleWebhookAsync(VikunjaWebhookPayload payload);
}

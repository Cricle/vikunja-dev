using Microsoft.Extensions.Logging;
using Vikunja.Core.Models;

namespace Vikunja.Core.Services;

public class DefaultWebhookHandler : WebhookHandlerBase
{
    public DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger) : base(logger)
    {
    }
}

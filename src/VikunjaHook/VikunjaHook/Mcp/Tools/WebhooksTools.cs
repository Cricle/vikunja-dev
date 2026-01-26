using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tools for managing webhooks
/// </summary>
internal class WebhooksTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<WebhooksTools> _logger;

    public WebhooksTools(
        IVikunjaClientFactory clientFactory,
        ILogger<WebhooksTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all webhooks for a project")]
    public async Task<List<VikunjaWebhook>> ListWebhooks(
        [Description("Project ID")] long projectId,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing webhooks for project {ProjectId}", projectId);

        var queryParams = $"page={page}&per_page={perPage}";
        var webhooks = await _clientFactory.GetAsync<List<VikunjaWebhook>>(
            $"projects/{projectId}/webhooks?{queryParams}",
            cancellationToken
        );

        return webhooks ?? new List<VikunjaWebhook>();
    }

    [McpServerTool]
    [Description("Create a new webhook for a project")]
    public async Task<VikunjaWebhook> CreateWebhook(
        [Description("Project ID")] long projectId,
        [Description("Target URL for webhook")] string targetUrl,
        [Description("Comma-separated list of events (e.g., task.created,task.updated)")] string events,
        [Description("Secret for webhook authentication (optional)")] string? secret = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating webhook for project {ProjectId}", projectId);

        var eventList = events.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var request = new
        {
            project_id = projectId,
            target_url = targetUrl,
            events = eventList,
            secret
        };

        var webhook = await _clientFactory.PutAsync<VikunjaWebhook>(
            $"projects/{projectId}/webhooks",
            request,
            cancellationToken
        );

        return webhook;
    }

    [McpServerTool]
    [Description("Get details of a specific webhook")]
    public async Task<VikunjaWebhook> GetWebhook(
        [Description("Project ID")] long projectId,
        [Description("Webhook ID")] long webhookId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting webhook {WebhookId} from project {ProjectId}", webhookId, projectId);

        var webhook = await _clientFactory.GetAsync<VikunjaWebhook>(
            $"projects/{projectId}/webhooks/{webhookId}",
            cancellationToken
        );

        return webhook;
    }

    [McpServerTool]
    [Description("Update an existing webhook")]
    public async Task<VikunjaWebhook> UpdateWebhook(
        [Description("Project ID")] long projectId,
        [Description("Webhook ID")] long webhookId,
        [Description("New target URL (optional)")] string? targetUrl = null,
        [Description("New comma-separated list of events (optional)")] string? events = null,
        [Description("New secret (optional)")] string? secret = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating webhook {WebhookId} in project {ProjectId}", webhookId, projectId);

        List<string>? eventList = null;
        if (!string.IsNullOrWhiteSpace(events))
        {
            eventList = events.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        var request = new
        {
            id = webhookId,
            target_url = targetUrl,
            events = eventList,
            secret
        };

        var webhook = await _clientFactory.PostAsync<VikunjaWebhook>(
            $"projects/{projectId}/webhooks/{webhookId}",
            request,
            cancellationToken
        );

        return webhook;
    }

    [McpServerTool]
    [Description("Delete a webhook")]
    public async Task<string> DeleteWebhook(
        [Description("Project ID")] long projectId,
        [Description("Webhook ID")] long webhookId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting webhook {WebhookId} from project {ProjectId}", webhookId, projectId);

        await _clientFactory.DeleteAsync(
            $"projects/{projectId}/webhooks/{webhookId}",
            cancellationToken
        );

        return $"Webhook {webhookId} deleted from project {projectId}";
    }
}

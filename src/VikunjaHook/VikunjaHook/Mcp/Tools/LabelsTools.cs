using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tools for managing Vikunja labels
/// </summary>
internal class LabelsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<LabelsTools> _logger;

    public LabelsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<LabelsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all labels")]
    public async Task<List<VikunjaLabel>> ListLabels(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        [Description("Search query (optional)")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing labels - page: {Page}", page);

        var queryParams = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"s={Uri.EscapeDataString(search)}");
        }

        var endpoint = $"labels?{string.Join("&", queryParams)}";
        var labels = await _clientFactory.GetAsync<List<VikunjaLabel>>(endpoint, cancellationToken);
        return labels ?? new List<VikunjaLabel>();
    }

    [McpServerTool]
    [Description("Create a new label")]
    public async Task<VikunjaLabel> CreateLabel(
        [Description("Label title")] string title,
        [Description("Label description (optional)")] string? description = null,
        [Description("Hex color code (optional, e.g., #FF5733)")] string? hexColor = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating label '{Title}'", title);

        var request = new
        {
            title,
            description,
            hex_color = hexColor?.ToLowerInvariant()
        };

        var label = await _clientFactory.PutAsync<VikunjaLabel>(
            "labels",
            request,
            cancellationToken
        );

        return label;
    }

    [McpServerTool]
    [Description("Get details of a specific label")]
    public async Task<VikunjaLabel> GetLabel(
        [Description("Label ID")] long labelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting label {LabelId}", labelId);

        var label = await _clientFactory.GetAsync<VikunjaLabel>(
            $"labels/{labelId}",
            cancellationToken
        );

        return label;
    }

    [McpServerTool]
    [Description("Update an existing label")]
    public async Task<VikunjaLabel> UpdateLabel(
        [Description("Label ID")] long labelId,
        [Description("New title (optional)")] string? title = null,
        [Description("New description (optional)")] string? description = null,
        [Description("New hex color code (optional)")] string? hexColor = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating label {LabelId}", labelId);

        var request = new
        {
            id = labelId,
            title,
            description,
            hex_color = hexColor?.ToLowerInvariant()
        };

        var label = await _clientFactory.PostAsync<VikunjaLabel>(
            $"labels/{labelId}",
            request,
            cancellationToken
        );

        return label;
    }

    [McpServerTool]
    [Description("Delete a label")]
    public async Task<string> DeleteLabel(
        [Description("Label ID")] long labelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting label {LabelId}", labelId);

        await _clientFactory.DeleteAsync(
            $"labels/{labelId}",
            cancellationToken
        );

        return $"Label {labelId} deleted successfully";
    }
}

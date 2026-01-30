using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing saved filters
/// </summary>
public class SavedFiltersTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<SavedFiltersTools> _logger;

    public SavedFiltersTools(
        IVikunjaClientFactory clientFactory,
        ILogger<SavedFiltersTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all saved filters")]
    public async Task<List<VikunjaSavedFilter>> ListSavedFilters(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing saved filters");

        var queryParams = $"page={page}&per_page={perPage}";
        var filters = await _clientFactory.GetAsync<List<VikunjaSavedFilter>>(
            $"filters?{queryParams}",
            cancellationToken
        );

        return filters ?? new List<VikunjaSavedFilter>();
    }

    [McpServerTool]
    [Description("Create a new saved filter")]
    public async Task<VikunjaSavedFilter> CreateSavedFilter(
        [Description("Filter title")] string title,
        [Description("Filter query string")] string filters,
        [Description("Filter description (optional)")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating saved filter '{Title}'", title);

        var request = new
        {
            title,
            description,
            filters
        };

        var filter = await _clientFactory.PutAsync<VikunjaSavedFilter>(
            "filters",
            request,
            cancellationToken
        );

        return filter;
    }

    [McpServerTool]
    [Description("Get details of a specific saved filter")]
    public async Task<VikunjaSavedFilter> GetSavedFilter(
        [Description("Filter ID")] long filterId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting saved filter {FilterId}", filterId);

        var filter = await _clientFactory.GetAsync<VikunjaSavedFilter>(
            $"filters/{filterId}",
            cancellationToken
        );

        return filter;
    }

    [McpServerTool]
    [Description("Update an existing saved filter")]
    public async Task<VikunjaSavedFilter> UpdateSavedFilter(
        [Description("Filter ID")] long filterId,
        [Description("New title (optional)")] string? title = null,
        [Description("New filter query (optional)")] string? filters = null,
        [Description("New description (optional)")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating saved filter {FilterId}", filterId);

        var request = new
        {
            id = filterId,
            title,
            description,
            filters
        };

        var filter = await _clientFactory.PostAsync<VikunjaSavedFilter>(
            $"filters/{filterId}",
            request,
            cancellationToken
        );

        return filter;
    }

    [McpServerTool]
    [Description("Delete a saved filter")]
    public async Task<string> DeleteSavedFilter(
        [Description("Filter ID")] long filterId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting saved filter {FilterId}", filterId);

        await _clientFactory.DeleteAsync(
            $"filters/{filterId}",
            cancellationToken
        );

        return $"Saved filter {filterId} deleted successfully";
    }
}

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tools for managing Vikunja teams
/// </summary>
internal class TeamsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TeamsTools> _logger;

    public TeamsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TeamsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all teams")]
    public async Task<List<VikunjaTeam>> ListTeams(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        [Description("Search query (optional)")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing teams - page: {Page}", page);

        var queryParams = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"s={Uri.EscapeDataString(search)}");
        }

        var endpoint = $"teams?{string.Join("&", queryParams)}";
        var teams = await _clientFactory.GetAsync<List<VikunjaTeam>>(endpoint, cancellationToken);
        return teams ?? new List<VikunjaTeam>();
    }

    [McpServerTool]
    [Description("Create a new team")]
    public async Task<VikunjaTeam> CreateTeam(
        [Description("Team name")] string name,
        [Description("Team description (optional)")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating team '{Name}'", name);

        var request = new
        {
            name,
            description
        };

        var team = await _clientFactory.PutAsync<VikunjaTeam>(
            "teams",
            request,
            cancellationToken
        );

        return team;
    }

    [McpServerTool]
    [Description("Get details of a specific team")]
    public async Task<VikunjaTeam> GetTeam(
        [Description("Team ID")] long teamId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting team {TeamId}", teamId);

        var team = await _clientFactory.GetAsync<VikunjaTeam>(
            $"teams/{teamId}",
            cancellationToken
        );

        return team;
    }

    [McpServerTool]
    [Description("Update an existing team")]
    public async Task<VikunjaTeam> UpdateTeam(
        [Description("Team ID")] long teamId,
        [Description("New name (optional)")] string? name = null,
        [Description("New description (optional)")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating team {TeamId}", teamId);

        var request = new
        {
            id = teamId,
            name,
            description
        };

        var team = await _clientFactory.PostAsync<VikunjaTeam>(
            $"teams/{teamId}",
            request,
            cancellationToken
        );

        return team;
    }

    [McpServerTool]
    [Description("Delete a team")]
    public async Task<string> DeleteTeam(
        [Description("Team ID")] long teamId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting team {TeamId}", teamId);

        await _clientFactory.DeleteAsync(
            $"teams/{teamId}",
            cancellationToken
        );

        return $"Team {teamId} deleted successfully";
    }
}

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing Vikunja users
/// </summary>
public class UsersTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<UsersTools> _logger;

    public UsersTools(
        IVikunjaClientFactory clientFactory,
        ILogger<UsersTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Get current user information")]
    public async Task<VikunjaUser> GetCurrentUser(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting current user information");

        var user = await _clientFactory.GetAsync<VikunjaUser>(
            "user",
            cancellationToken
        );

        return user;
    }

    [McpServerTool]
    [Description("Search for users")]
    public async Task<List<VikunjaUser>> SearchUsers(
        [Description("Search query")] string search,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching users with query: {Search}", search);

        var queryParams = new List<string>
        {
            $"s={Uri.EscapeDataString(search)}",
            $"page={page}",
            $"per_page={perPage}"
        };

        var endpoint = $"users?{string.Join("&", queryParams)}";
        var users = await _clientFactory.GetAsync<List<VikunjaUser>>(endpoint, cancellationToken);
        return users ?? new List<VikunjaUser>();
    }

    [McpServerTool]
    [Description("Get details of a specific user")]
    public async Task<VikunjaUser> GetUser(
        [Description("User ID")] long userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user {UserId}", userId);

        var user = await _clientFactory.GetAsync<VikunjaUser>(
            $"users/{userId}",
            cancellationToken
        );

        return user;
    }
}

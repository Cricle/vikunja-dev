using System.Text.Json;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tool for managing Vikunja users (JWT authentication only)
/// </summary>
public class UsersTool : IMcpTool
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly IAuthenticationManager _authManager;
    private readonly ILogger<UsersTool> _logger;

    public string Name => "users";
    public string Description => "Manage Vikunja users: get current user, search users, manage settings (JWT only)";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "current",
        "search",
        "settings",
        "update-settings"
    };

    public UsersTool(
        IVikunjaClientFactory clientFactory,
        IAuthenticationManager authManager,
        ILogger<UsersTool> logger)
    {
        _clientFactory = clientFactory;
        _authManager = authManager;
        _logger = logger;
    }

    public async Task<object?> ExecuteAsync(
        string subcommand,
        Dictionary<string, object?> parameters,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = _authManager.GetSession(sessionId);
        if (session == null)
        {
            throw new AuthenticationException("Invalid session");
        }

        // Check if session uses JWT authentication
        if (session.AuthType != AuthType.Jwt)
        {
            throw new AuthenticationException("User operations require JWT authentication. API tokens are not supported for user management.");
        }

        return subcommand switch
        {
            "current" => await GetCurrentUserAsync(session, cancellationToken),
            "search" => await SearchUsersAsync(session, parameters, cancellationToken),
            "settings" => await GetUserSettingsAsync(session, cancellationToken),
            "update-settings" => await UpdateUserSettingsAsync(session, parameters, cancellationToken),
            _ => throw new ValidationException($"Unknown subcommand: {subcommand}")
        };
    }

    private async Task<object> GetCurrentUserAsync(
        AuthSession session,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting current user for session {SessionId}", session.SessionId);

        var user = await _clientFactory.GetAsync<VikunjaUser>(
            session,
            "/user",
            cancellationToken
        );

        if (user == null)
        {
            throw new McpException(McpErrorCode.OperationFailed, "Failed to retrieve current user information");
        }

        return new
        {
            user,
            message = "Current user information retrieved successfully"
        };
    }

    private async Task<object> SearchUsersAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var searchQuery = GetStringParameter(parameters, "query")
            ?? throw new ValidationException("query is required for user search");

        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            throw new ValidationException("search query cannot be empty");
        }

        _logger.LogInformation("Searching users with query '{Query}' for session {SessionId}",
            searchQuery, session.SessionId);

        var endpoint = $"users?s={Uri.EscapeDataString(searchQuery)}";

        var users = await _clientFactory.GetAsync<List<VikunjaUser>>(
            session,
            endpoint,
            cancellationToken
        );

        return new
        {
            users = users ?? new List<VikunjaUser>(),
            count = users?.Count ?? 0,
            query = searchQuery
        };
    }

    private async Task<object> GetUserSettingsAsync(
        AuthSession session,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user settings for session {SessionId}", session.SessionId);

        var settings = await _clientFactory.GetAsync<Dictionary<string, object?>>(
            session,
            "/user/settings",
            cancellationToken
        );

        return new
        {
            settings = settings ?? new Dictionary<string, object?>(),
            message = "User settings retrieved successfully"
        };
    }

    private async Task<object> UpdateUserSettingsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user settings for session {SessionId}", session.SessionId);

        // Extract settings to update
        if (!parameters.TryGetValue("settings", out var settingsObj) || settingsObj == null)
        {
            throw new ValidationException("settings object is required");
        }

        Dictionary<string, object?> settingsToUpdate;

        if (settingsObj is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            settingsToUpdate = new Dictionary<string, object?>();
            foreach (var property in element.EnumerateObject())
            {
                settingsToUpdate[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var num) ? num : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString()
                };
            }
        }
        else if (settingsObj is Dictionary<string, object?> dict)
        {
            settingsToUpdate = dict;
        }
        else
        {
            throw new ValidationException("settings must be a valid object");
        }

        if (settingsToUpdate.Count == 0)
        {
            throw new ValidationException("settings object must contain at least one setting to update");
        }

        var updatedSettings = await _clientFactory.PostAsync<Dictionary<string, object?>>(
            session,
            "/user/settings",
            settingsToUpdate,
            cancellationToken
        );

        return new
        {
            settings = updatedSettings ?? new Dictionary<string, object?>(),
            message = "User settings updated successfully"
        };
    }

    // Helper methods for parameter extraction
    private static string? GetStringParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            return value is JsonElement element && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : value.ToString();
        }
        return null;
    }
}

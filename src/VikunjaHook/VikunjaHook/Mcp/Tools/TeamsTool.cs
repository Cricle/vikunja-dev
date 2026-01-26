using System.Text.Json;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tool for managing Vikunja teams
/// </summary>
public class TeamsTool : IMcpTool
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly IAuthenticationManager _authManager;
    private readonly ILogger<TeamsTool> _logger;

    public string Name => "teams";
    public string Description => "Manage Vikunja teams: create, delete, list";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "list",
        "create",
        "delete"
    };

    public TeamsTool(
        IVikunjaClientFactory clientFactory,
        IAuthenticationManager authManager,
        ILogger<TeamsTool> logger)
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

        return subcommand switch
        {
            "list" => await ListTeamsAsync(session, parameters, cancellationToken),
            "create" => await CreateTeamAsync(session, parameters, cancellationToken),
            "delete" => await DeleteTeamAsync(session, parameters, cancellationToken),
            _ => throw new ValidationException($"Unknown subcommand: {subcommand}")
        };
    }

    private async Task<object> ListTeamsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing teams for session {SessionId}", session.SessionId);

        // Extract query parameters
        var page = GetIntParameter(parameters, "page", 1);
        var perPage = GetIntParameter(parameters, "perPage", 50);
        var search = GetStringParameter(parameters, "search");

        // Build query string
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

        var teams = await _clientFactory.GetAsync<List<VikunjaTeam>>(
            session,
            endpoint,
            cancellationToken
        );

        return new
        {
            teams = teams ?? new List<VikunjaTeam>(),
            count = teams?.Count ?? 0,
            page,
            perPage
        };
    }

    private async Task<object> CreateTeamAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating team for session {SessionId}", session.SessionId);

        // Extract and validate required parameters
        var name = GetStringParameter(parameters, "name")
            ?? throw new ValidationException("name is required");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("name cannot be empty");
        }

        // Extract optional parameters
        var description = GetStringParameter(parameters, "description");

        // Build request
        var request = new Dictionary<string, object?>
        {
            ["name"] = name.Trim()
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            request["description"] = description.Trim();
        }

        var team = await _clientFactory.PutAsync<VikunjaTeam>(
            session,
            "teams",
            request,
            cancellationToken
        );

        return new
        {
            team,
            message = $"Team '{name}' created successfully"
        };
    }

    private async Task<object> DeleteTeamAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var teamId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Deleting team {TeamId} for session {SessionId}",
            teamId, session.SessionId);

        // Get team details before deletion
        var team = await _clientFactory.GetAsync<VikunjaTeam>(
            session,
            $"teams/{teamId}",
            cancellationToken
        );

        if (team == null)
        {
            throw new ResourceNotFoundException("Team", teamId);
        }

        await _clientFactory.DeleteAsync(
            session,
            $"teams/{teamId}",
            cancellationToken
        );

        return new
        {
            message = $"Deleted team: {team.Name}",
            deleted = true,
            teamId,
            teamName = team.Name
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

    private static long? GetLongParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var num))
                    return num;
            }
            else if (value is long l)
                return l;
            else if (value is int i)
                return i;
            else if (long.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static int? GetIntParameter(Dictionary<string, object?> parameters, string key, int? defaultValue = null)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var num))
                    return num;
            }
            else if (value is int i)
                return i;
            else if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }
}

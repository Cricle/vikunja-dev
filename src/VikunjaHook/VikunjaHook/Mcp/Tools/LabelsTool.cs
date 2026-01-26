using System.Text.Json;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Models.Requests;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tool for managing Vikunja labels
/// </summary>
public class LabelsTool : IMcpTool
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly IAuthenticationManager _authManager;
    private readonly ILogger<LabelsTool> _logger;

    public string Name => "labels";
    public string Description => "Manage Vikunja labels: create, get, update, delete, list";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "list",
        "create",
        "get",
        "update",
        "delete"
    };

    public LabelsTool(
        IVikunjaClientFactory clientFactory,
        IAuthenticationManager authManager,
        ILogger<LabelsTool> logger)
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
            "list" => await ListLabelsAsync(session, parameters, cancellationToken),
            "create" => await CreateLabelAsync(session, parameters, cancellationToken),
            "get" => await GetLabelAsync(session, parameters, cancellationToken),
            "update" => await UpdateLabelAsync(session, parameters, cancellationToken),
            "delete" => await DeleteLabelAsync(session, parameters, cancellationToken),
            _ => throw new ValidationException($"Unknown subcommand: {subcommand}")
        };
    }

    private async Task<object> ListLabelsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing labels for session {SessionId}", session.SessionId);

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

        var endpoint = $"labels?{string.Join("&", queryParams)}";

        var labels = await _clientFactory.GetAsync<List<VikunjaLabel>>(
            session,
            endpoint,
            cancellationToken
        );

        return new
        {
            labels = labels ?? new List<VikunjaLabel>(),
            count = labels?.Count ?? 0,
            page,
            perPage
        };
    }

    private async Task<object> CreateLabelAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating label for session {SessionId}", session.SessionId);

        // Extract and validate required parameters
        var title = GetStringParameter(parameters, "title")
            ?? throw new ValidationException("title is required");

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("title cannot be empty");
        }

        // Extract optional parameters
        var description = GetStringParameter(parameters, "description");
        var hexColor = GetStringParameter(parameters, "hexColor");

        // Validate hex color if provided
        if (!string.IsNullOrWhiteSpace(hexColor))
        {
            ValidateHexColor(hexColor);
        }

        // Build request
        var request = new CreateLabelRequest(
            Title: title.Trim(),
            Description: description?.Trim(),
            HexColor: hexColor?.ToLowerInvariant()
        );

        var label = await _clientFactory.PutAsync<VikunjaLabel>(
            session,
            "labels",
            request,
            cancellationToken
        );

        return new
        {
            label,
            message = $"Label '{title}' created successfully"
        };
    }

    private async Task<object> GetLabelAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var labelId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Getting label {LabelId} for session {SessionId}",
            labelId, session.SessionId);

        var label = await _clientFactory.GetAsync<VikunjaLabel>(
            session,
            $"labels/{labelId}",
            cancellationToken
        );

        if (label == null)
        {
            throw new ResourceNotFoundException("Label", labelId);
        }

        return new { label };
    }

    private async Task<object> UpdateLabelAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var labelId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Updating label {LabelId} for session {SessionId}",
            labelId, session.SessionId);

        // Extract optional update fields
        var title = GetStringParameter(parameters, "title");
        var description = GetStringParameter(parameters, "description");
        var hexColor = GetStringParameter(parameters, "hexColor");

        // Check if at least one field to update is provided
        if (title == null && description == null && hexColor == null)
        {
            throw new ValidationException("At least one field to update must be provided");
        }

        // Validate hex color if provided
        if (!string.IsNullOrWhiteSpace(hexColor))
        {
            ValidateHexColor(hexColor);
        }

        // Build update request with only provided fields
        var updateData = new Dictionary<string, object?>();

        if (title != null)
        {
            updateData["title"] = title.Trim();
        }

        if (description != null)
        {
            updateData["description"] = description.Trim();
        }

        if (hexColor != null)
        {
            updateData["hex_color"] = hexColor.ToLowerInvariant();
        }

        var label = await _clientFactory.PutAsync<VikunjaLabel>(
            session,
            $"labels/{labelId}",
            updateData,
            cancellationToken
        );

        return new
        {
            label,
            message = "Label updated successfully"
        };
    }

    private async Task<object> DeleteLabelAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var labelId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Deleting label {LabelId} for session {SessionId}",
            labelId, session.SessionId);

        // Get label details before deletion
        var label = await _clientFactory.GetAsync<VikunjaLabel>(
            session,
            $"labels/{labelId}",
            cancellationToken
        );

        if (label == null)
        {
            throw new ResourceNotFoundException("Label", labelId);
        }

        await _clientFactory.DeleteAsync(
            session,
            $"labels/{labelId}",
            cancellationToken
        );

        return new
        {
            message = $"Deleted label: {label.Title}",
            deleted = true,
            labelId,
            labelTitle = label.Title
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

    private static void ValidateHexColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            throw new ValidationException("Hex color cannot be empty");
        }

        // Must start with # and be followed by exactly 6 hex digits
        if (!System.Text.RegularExpressions.Regex.IsMatch(hexColor, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new ValidationException($"Invalid hex color format: {hexColor}. Expected format: #RRGGBB");
        }
    }
}

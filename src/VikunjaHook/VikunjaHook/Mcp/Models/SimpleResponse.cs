namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Simple success response with a message
/// </summary>
public record SuccessResponse(
    string Message
);

/// <summary>
/// Webhook success response
/// </summary>
public record WebhookSuccessResponse(
    string Status,
    string EventName
);

/// <summary>
/// Health check response
/// </summary>
public record HealthResponse(
    string Status,
    DateTime Timestamp,
    string Service
);

/// <summary>
/// Supported events response
/// </summary>
public record SupportedEventsResponse(
    string[] SupportedEvents
);

/// <summary>
/// Tool execution response
/// </summary>
public record ToolExecutionResponse(
    bool Success,
    string Tool,
    string Subcommand,
    object? Data
);

/// <summary>
/// Tool info for listing
/// </summary>
public record ToolInfo(
    string Name,
    string Description,
    IReadOnlyList<string> Subcommands
);

/// <summary>
/// Tools list response
/// </summary>
public record ToolsListResponse(
    List<ToolInfo> Tools,
    int Count
);

/// <summary>
/// MCP health response
/// </summary>
public record McpHealthResponse(
    string Status,
    DateTime Timestamp,
    string ServerName,
    string Version
);

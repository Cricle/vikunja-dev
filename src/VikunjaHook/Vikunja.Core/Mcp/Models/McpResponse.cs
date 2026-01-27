namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a standard MCP response
/// </summary>
public record McpResponse(
    bool Success,
    string Operation,
    string? Message = null,
    object? Data = null,
    Dictionary<string, object?>? Metadata = null,
    McpError? Error = null
);

namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents an error in an MCP response
/// </summary>
public record McpError(
    string Code,
    string Message,
    Dictionary<string, object?>? Details = null
);

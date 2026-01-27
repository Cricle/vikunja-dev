namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents MCP server metadata
/// </summary>
public record McpServerInfo(
    string Name,
    string Version,
    Dictionary<string, object?> Capabilities
);

namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents an MCP request from a client
/// </summary>
public record McpRequest(
    string Operation,
    Dictionary<string, object?>? Parameters = null,
    string? SessionId = null
);

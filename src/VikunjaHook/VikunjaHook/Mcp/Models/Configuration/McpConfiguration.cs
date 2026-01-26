namespace VikunjaHook.Mcp.Models.Configuration;

/// <summary>
/// Configuration for MCP server
/// </summary>
public class McpConfiguration
{
    public string ServerName { get; set; } = "vikunja-mcp";
    public string Version { get; set; } = "1.0.0";
    public int MaxConcurrentConnections { get; set; } = 100;
}

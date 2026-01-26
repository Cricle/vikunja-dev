using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Tools;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// MCP server interface for handling tool requests
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// Register a tool with the server
    /// </summary>
    /// <param name="tool">The tool to register</param>
    void RegisterTool(IMcpTool tool);

    /// <summary>
    /// Get server information
    /// </summary>
    /// <returns>Server metadata</returns>
    McpServerInfo GetServerInfo();

    /// <summary>
    /// Handle an MCP request
    /// </summary>
    /// <param name="request">The MCP request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MCP response</returns>
    Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default);
}

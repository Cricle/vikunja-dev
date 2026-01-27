using Vikunja.Core.Mcp.Tools;

namespace Vikunja.Core.Mcp.Services;

/// <summary>
/// Registry for managing MCP tools
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool in the registry
    /// </summary>
    /// <param name="tool">The tool to register</param>
    void Register(IMcpTool tool);

    /// <summary>
    /// Get a tool by name
    /// </summary>
    /// <param name="name">The tool name</param>
    /// <returns>The tool if found, null otherwise</returns>
    IMcpTool? GetTool(string name);

    /// <summary>
    /// Get all registered tools
    /// </summary>
    /// <returns>Collection of all registered tools</returns>
    IReadOnlyCollection<IMcpTool> GetAllTools();
}

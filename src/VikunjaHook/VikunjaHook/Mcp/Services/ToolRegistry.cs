using System.Collections.Concurrent;
using VikunjaHook.Mcp.Tools;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Thread-safe registry for managing MCP tools
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, IMcpTool> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    public void Register(IMcpTool tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new ArgumentException("Tool name cannot be null or empty", nameof(tool));
        }

        if (_tools.TryAdd(tool.Name, tool))
        {
            _logger.LogInformation("Registered tool: {ToolName} with {SubcommandCount} subcommands",
                tool.Name, tool.Subcommands.Count);
        }
        else
        {
            _logger.LogWarning("Tool {ToolName} is already registered, skipping", tool.Name);
        }
    }

    public IMcpTool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    public IReadOnlyCollection<IMcpTool> GetAllTools()
    {
        return _tools.Values.ToList();
    }
}

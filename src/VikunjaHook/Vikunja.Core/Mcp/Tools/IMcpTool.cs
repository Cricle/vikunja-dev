namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// Interface for MCP tools that can be registered and executed
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// The name of the tool (e.g., "tasks", "projects")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the tool does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// List of available subcommands for this tool
    /// </summary>
    IReadOnlyList<string> Subcommands { get; }

    /// <summary>
    /// Execute a tool operation
    /// </summary>
    /// <param name="subcommand">The subcommand to execute</param>
    /// <param name="parameters">Parameters for the operation</param>
    /// <param name="sessionId">Authentication session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<object?> ExecuteAsync(
        string subcommand,
        Dictionary<string, object?> parameters,
        string sessionId,
        CancellationToken cancellationToken = default);
}

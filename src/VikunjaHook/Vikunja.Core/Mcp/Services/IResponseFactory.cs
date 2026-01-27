using Vikunja.Core.Mcp.Models;

namespace Vikunja.Core.Mcp.Services;

/// <summary>
/// Factory for creating standard MCP responses
/// </summary>
public interface IResponseFactory
{
    /// <summary>
    /// Create standard success response
    /// </summary>
    McpResponse CreateSuccess(
        string operation,
        string? message = null,
        object? data = null,
        Dictionary<string, object?>? metadata = null
    );
    
    /// <summary>
    /// Create standard error response
    /// </summary>
    McpResponse CreateError(
        string operation,
        string errorCode,
        string message,
        Dictionary<string, object?>? details = null
    );
    
    /// <summary>
    /// Create response from exception
    /// </summary>
    McpResponse CreateFromException(string operation, Exception exception);
}

namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Base exception for MCP operations
/// </summary>
public class McpException : Exception
{
    public McpErrorCode ErrorCode { get; }
    public Dictionary<string, object?>? Details { get; }
    
    public McpException(McpErrorCode errorCode, string message, Dictionary<string, object?>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

/// <summary>
/// Exception for authentication errors
/// </summary>
public class AuthenticationException : McpException
{
    public AuthenticationException(string message, Dictionary<string, object?>? details = null)
        : base(McpErrorCode.AuthenticationRequired, message, details)
    {
    }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : McpException
{
    public ValidationException(string message, Dictionary<string, object?>? details = null)
        : base(McpErrorCode.ValidationError, message, details)
    {
    }
}

/// <summary>
/// Exception for resource not found errors
/// </summary>
public class ResourceNotFoundException : McpException
{
    public ResourceNotFoundException(string resourceType, long id)
        : base(McpErrorCode.ResourceNotFound, $"{resourceType} with ID {id} not found")
    {
    }
}

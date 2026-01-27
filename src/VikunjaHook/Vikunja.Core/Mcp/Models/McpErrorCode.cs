namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Error codes for MCP operations
/// </summary>
public enum McpErrorCode
{
    // Authentication Errors (1xxx)
    AuthenticationRequired = 1001,
    InvalidToken = 1002,
    TokenExpired = 1003,
    InvalidSession = 1004,
    InsufficientPermissions = 1005,
    
    // Validation Errors (2xxx)
    ValidationError = 2001,
    InvalidDateFormat = 2002,
    InvalidHexColor = 2003,
    InvalidPriority = 2004,
    RequiredFieldMissing = 2005,
    InvalidRequest = 2006,
    
    // Resource Errors (3xxx)
    ResourceNotFound = 3001,
    ResourceAlreadyExists = 3002,
    ResourceDeleted = 3003,
    
    // Operation Errors (4xxx)
    OperationFailed = 4001,
    NetworkError = 4002,
    TimeoutError = 4003,
    RateLimitExceeded = 4004,
    ToolNotFound = 4005,
    
    // Server Errors (5xxx)
    InternalError = 5001,
    NotImplemented = 5002,
    ServiceUnavailable = 5003
}

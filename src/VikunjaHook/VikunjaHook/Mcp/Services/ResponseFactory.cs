using System.Text.RegularExpressions;
using VikunjaHook.Mcp.Models;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Factory for creating standard MCP responses with sensitive data masking
/// </summary>
public partial class ResponseFactory : IResponseFactory
{
    private readonly ILogger<ResponseFactory> _logger;

    // Regex patterns for sensitive data (compiled for performance)
    [GeneratedRegex(@"tk_[a-zA-Z0-9]+", RegexOptions.Compiled)]
    private static partial Regex ApiTokenPattern();
    
    [GeneratedRegex(@"eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtTokenPattern();
    
    [GeneratedRegex(@"Bearer\s+[a-zA-Z0-9_\-\.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenPattern();

    public ResponseFactory(ILogger<ResponseFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create standard success response
    /// </summary>
    public McpResponse CreateSuccess(
        string operation,
        string? message = null,
        object? data = null,
        Dictionary<string, object?>? metadata = null)
    {
        var responseMetadata = metadata ?? new Dictionary<string, object?>();
        responseMetadata["timestamp"] = DateTime.UtcNow.ToString("O");

        return new McpResponse(
            Success: true,
            Operation: operation,
            Message: message,
            Data: data,
            Metadata: responseMetadata,
            Error: null
        );
    }

    /// <summary>
    /// Create standard error response
    /// </summary>
    public McpResponse CreateError(
        string operation,
        string errorCode,
        string message,
        Dictionary<string, object?>? details = null)
    {
        // Mask sensitive data in error message and details
        var maskedMessage = MaskSensitiveData(message);
        var maskedDetails = details != null ? MaskSensitiveDataInDictionary(details) : null;

        var error = new McpError(
            Code: errorCode,
            Message: maskedMessage,
            Details: maskedDetails
        );

        var metadata = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        return new McpResponse(
            Success: false,
            Operation: operation,
            Message: maskedMessage,
            Data: null,
            Metadata: metadata,
            Error: error
        );
    }

    /// <summary>
    /// Create response from exception
    /// </summary>
    public McpResponse CreateFromException(string operation, Exception exception)
    {
        _logger.LogError(exception, "Exception occurred during operation {Operation}", operation);

        return exception switch
        {
            McpException mcpEx => CreateError(
                operation,
                mcpEx.ErrorCode.ToString(),
                mcpEx.Message,
                mcpEx.Details
            ),
            ArgumentException argEx => CreateError(
                operation,
                McpErrorCode.ValidationError.ToString(),
                argEx.Message,
                new Dictionary<string, object?>
                {
                    ["parameterName"] = argEx.ParamName
                }
            ),
            UnauthorizedAccessException => CreateError(
                operation,
                McpErrorCode.InsufficientPermissions.ToString(),
                "Access denied"
            ),
            TimeoutException => CreateError(
                operation,
                McpErrorCode.TimeoutError.ToString(),
                "Operation timed out"
            ),
            _ => CreateError(
                operation,
                McpErrorCode.InternalError.ToString(),
                "An unexpected error occurred",
                new Dictionary<string, object?>
                {
                    ["exceptionType"] = exception.GetType().Name
                }
            )
        };
    }

    /// <summary>
    /// Mask sensitive data in a string (tokens, passwords, etc.)
    /// </summary>
    private static string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask API tokens (tk_xxx)
        var masked = ApiTokenPattern().Replace(input, "tk_***");
        
        // Mask JWT tokens
        masked = JwtTokenPattern().Replace(masked, "eyJ***");
        
        // Mask Bearer tokens
        masked = BearerTokenPattern().Replace(masked, "Bearer ***");

        return masked;
    }

    /// <summary>
    /// Mask sensitive data in a dictionary
    /// </summary>
    private static Dictionary<string, object?> MaskSensitiveDataInDictionary(Dictionary<string, object?> dict)
    {
        var masked = new Dictionary<string, object?>();

        foreach (var kvp in dict)
        {
            var key = kvp.Key.ToLowerInvariant();
            
            // Mask known sensitive keys
            if (key.Contains("token") || key.Contains("password") || key.Contains("secret") || key.Contains("key"))
            {
                masked[kvp.Key] = "***";
            }
            else if (kvp.Value is string strValue)
            {
                masked[kvp.Key] = MaskSensitiveData(strValue);
            }
            else if (kvp.Value is Dictionary<string, object?> nestedDict)
            {
                masked[kvp.Key] = MaskSensitiveDataInDictionary(nestedDict);
            }
            else
            {
                masked[kvp.Key] = kvp.Value;
            }
        }

        return masked;
    }
}

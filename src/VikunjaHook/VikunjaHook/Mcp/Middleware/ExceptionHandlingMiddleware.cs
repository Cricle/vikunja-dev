using System.Net;
using System.Text.Json;
using VikunjaHook.Mcp.Models;

namespace VikunjaHook.Mcp.Middleware;

/// <summary>
/// Middleware for handling unhandled exceptions and converting them to standard error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        HttpStatusCode statusCode;
        McpErrorCode errorCode;
        string message;

        switch (exception)
        {
            case AuthenticationException:
                statusCode = HttpStatusCode.Unauthorized;
                errorCode = McpErrorCode.AuthenticationRequired;
                message = exception.Message;
                break;
            case ValidationException:
                statusCode = HttpStatusCode.BadRequest;
                errorCode = McpErrorCode.ValidationError;
                message = exception.Message;
                break;
            case ResourceNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                errorCode = McpErrorCode.ResourceNotFound;
                message = exception.Message;
                break;
            case McpException mcpEx:
                statusCode = HttpStatusCode.InternalServerError;
                errorCode = mcpEx.ErrorCode;
                message = mcpEx.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                errorCode = McpErrorCode.InternalError;
                message = "An unexpected error occurred";
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            Success = false,
            Error = new ErrorDetail
            {
                Code = errorCode.ToString(),
                Message = message,
                Timestamp = DateTime.UtcNow
            }
        };

        // Use source-generated JSON serialization for AOT compatibility
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.ErrorResponse));
    }
}

/// <summary>
/// Extension methods for registering the exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

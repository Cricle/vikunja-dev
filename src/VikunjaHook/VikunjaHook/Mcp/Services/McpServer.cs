using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Tools;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// MCP server implementation for handling tool requests
/// </summary>
public class McpServer : IMcpServer
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IAuthenticationManager _authManager;
    private readonly IResponseFactory _responseFactory;
    private readonly ILogger<McpServer> _logger;
    private readonly McpServerInfo _serverInfo;

    public McpServer(
        IToolRegistry toolRegistry,
        IAuthenticationManager authManager,
        IResponseFactory responseFactory,
        ILogger<McpServer> logger)
    {
        _toolRegistry = toolRegistry;
        _authManager = authManager;
        _responseFactory = responseFactory;
        _logger = logger;

        _serverInfo = new McpServerInfo(
            Name: "vikunja-mcp-csharp",
            Version: "1.0.0",
            Capabilities: new Dictionary<string, object?>
            {
                ["tools"] = true,
                ["authentication"] = new[] { "api_token", "jwt" }
            }
        );
    }

    public void RegisterTool(IMcpTool tool)
    {
        _toolRegistry.Register(tool);
    }

    public McpServerInfo GetServerInfo()
    {
        return _serverInfo;
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate authentication
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                _logger.LogWarning("Request missing session ID");
                return _responseFactory.CreateError(
                    request.Operation,
                    McpErrorCode.AuthenticationRequired.ToString(),
                    "Session ID is required"
                );
            }

            var session = _authManager.GetSession(request.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Invalid session ID: {SessionId}", request.SessionId);
                return _responseFactory.CreateError(
                    request.Operation,
                    McpErrorCode.InvalidSession.ToString(),
                    "Invalid or expired session"
                );
            }

            // Parse operation (format: "tool.subcommand")
            var parts = request.Operation.Split('.', 2);
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid operation format: {Operation}", request.Operation);
                return _responseFactory.CreateError(
                    request.Operation,
                    McpErrorCode.InvalidRequest.ToString(),
                    "Operation must be in format 'tool.subcommand'"
                );
            }

            var toolName = parts[0];
            var subcommand = parts[1];

            // Get tool
            var tool = _toolRegistry.GetTool(toolName);
            if (tool == null)
            {
                _logger.LogWarning("Tool not found: {ToolName}", toolName);
                return _responseFactory.CreateError(
                    request.Operation,
                    McpErrorCode.ToolNotFound.ToString(),
                    $"Tool '{toolName}' not found"
                );
            }

            // Execute tool
            _logger.LogInformation("Executing {Operation} for session {SessionId}",
                request.Operation, request.SessionId);

            var result = await tool.ExecuteAsync(
                subcommand,
                request.Parameters ?? new Dictionary<string, object?>(),
                request.SessionId,
                cancellationToken
            );

            return _responseFactory.CreateSuccess(request.Operation, data: result);
        }
        catch (McpException ex)
        {
            _logger.LogError(ex, "MCP error handling request: {Operation}", request.Operation);
            return _responseFactory.CreateFromException(request.Operation, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling request: {Operation}", request.Operation);
            return _responseFactory.CreateError(
                request.Operation,
                McpErrorCode.InternalError.ToString(),
                "An unexpected error occurred",
                new Dictionary<string, object?> { ["exception"] = ex.Message }
            );
        }
    }
}

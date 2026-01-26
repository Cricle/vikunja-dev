using System.Text.Json;
using System.Threading.RateLimiting;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;
using VikunjaHook;
using VikunjaHook.Models;
using VikunjaHook.Services;
using VikunjaHook.Mcp.Services;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Tools;
using VikunjaHook.Mcp.Middleware;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "VikunjaHook")
    .Enrich.With<SensitiveDataMaskingEnricher>()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/vikunja-mcp-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting VikunjaHook MCP Server");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add controllers for API endpoints
// Note: MVC/Controllers don't fully support trimming/AOT yet, but we use minimal APIs where possible
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Controllers are required for configuration endpoints")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Controllers are required for configuration endpoints")]
static void AddControllersWithSuppression(IServiceCollection services)
{
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });
}

AddControllersWithSuppression(builder.Services);

// 配置JSON序列化（支持AOT）
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 注册webhook处理服务
builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();

// 注册MCP服务
builder.Services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddSingleton<IResponseFactory, ResponseFactory>();
builder.Services.AddSingleton<IVikunjaClientFactory, VikunjaClientFactory>();
builder.Services.AddSingleton<IMcpServer, McpServer>();
builder.Services.AddSingleton<ConfigurationValidator>();
builder.Services.AddHttpClient();

// 注册MCP工具
builder.Services.AddSingleton<TasksTool>();
builder.Services.AddSingleton<ProjectsTool>();
builder.Services.AddSingleton<LabelsTool>();
builder.Services.AddSingleton<TeamsTool>();
builder.Services.AddSingleton<UsersTool>();

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Extract session ID from Authorization header
        var authHeader = context.Request.Headers.Authorization.ToString();
        var partitionKey = "anonymous";
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            partitionKey = authHeader.Substring("Bearer ".Length).Trim();
        }
        
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue<int>("RateLimit:RequestsPerMinute", 60),
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        var errorResponse = new Dictionary<string, object?>
        {
            ["error"] = "Rate limit exceeded"
        };
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            errorResponse["retryAfter"] = retryAfter.TotalSeconds;
        }
        
        var json = System.Text.Json.JsonSerializer.Serialize(errorResponse, AppJsonSerializerContext.Default.DictionaryStringObject);
        await context.HttpContext.Response.WriteAsync(json, cancellationToken);
    };
});

var app = builder.Build();

// Validate configuration on startup
var configValidator = app.Services.GetRequiredService<ConfigurationValidator>();
if (!configValidator.ValidateConfiguration(builder.Configuration))
{
    Log.Fatal("Configuration validation failed. Application cannot start.");
    return 1;
}

// Use exception handling middleware
app.UseExceptionHandling();

// Use CORS
app.UseCors();

// Use Rate Limiting
app.UseRateLimiter();

// Serve static files from wwwroot (for frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

// Log startup information
Log.Information("VikunjaHook MCP Server started successfully");
Log.Information("Registered {ToolCount} MCP tools", app.Services.GetRequiredService<IToolRegistry>().GetAllTools().Count);

// Register shutdown handler
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is shutting down...");
    
    // Clear all authentication sessions
    var authManager = app.Services.GetRequiredService<IAuthenticationManager>();
    authManager.DisconnectAll();
    
    Log.Information("Resource cleanup completed");
});

// 注册工具到MCP服务器
var mcpServer = app.Services.GetRequiredService<IMcpServer>();
var tasksTool = app.Services.GetRequiredService<TasksTool>();
var projectsTool = app.Services.GetRequiredService<ProjectsTool>();
var labelsTool = app.Services.GetRequiredService<LabelsTool>();
var teamsTool = app.Services.GetRequiredService<TeamsTool>();
var usersTool = app.Services.GetRequiredService<UsersTool>();
mcpServer.RegisterTool(tasksTool);
mcpServer.RegisterTool(projectsTool);
mcpServer.RegisterTool(labelsTool);
mcpServer.RegisterTool(teamsTool);
mcpServer.RegisterTool(usersTool);

// Vikunja webhook端点
app.MapPost("/webhook/vikunja", async (
    HttpContext context,
    IWebhookHandler handler,
    ILogger<Program> logger) =>
{
    try
    {
        var payload = await context.Request.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.VikunjaWebhookPayload);

        if (payload == null)
        {
            logger.LogWarning("收到空的webhook payload");
            return Results.BadRequest(new ErrorMessage("Invalid payload"));
        }

        if (!VikunjaEventTypes.IsValidEvent(payload.EventName))
        {
            logger.LogWarning("收到未知的事件类型: {EventName}", payload.EventName);
            return Results.BadRequest(new ErrorMessage("Unknown event type"));
        }

        await handler.HandleWebhookAsync(payload);

        return Results.Ok(new WebhookSuccessResponse("success", payload.EventName));
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "JSON解析错误");
        return Results.BadRequest(new ErrorMessage("Invalid JSON format"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "处理webhook时发生错误");
        return Results.Problem("Internal server error");
    }
});

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new HealthResponse(
    "healthy",
    DateTime.UtcNow,
    "VikunjaHook"
)));

// 支持的事件列表端点
app.MapGet("/webhook/vikunja/events", () => Results.Ok(new SupportedEventsResponse(
    new[]
    {
        VikunjaEventTypes.TaskCreated,
        VikunjaEventTypes.TaskUpdated,
        VikunjaEventTypes.TaskDeleted,
        VikunjaEventTypes.ProjectCreated,
        VikunjaEventTypes.ProjectUpdated,
        VikunjaEventTypes.ProjectDeleted,
        VikunjaEventTypes.TaskAssigneeCreated,
        VikunjaEventTypes.TaskAssigneeDeleted,
        VikunjaEventTypes.TaskCommentCreated,
        VikunjaEventTypes.TaskCommentUpdated,
        VikunjaEventTypes.TaskCommentDeleted,
        VikunjaEventTypes.TaskAttachmentCreated,
        VikunjaEventTypes.TaskAttachmentDeleted,
        VikunjaEventTypes.TaskRelationCreated,
        VikunjaEventTypes.TaskRelationDeleted,
        VikunjaEventTypes.LabelCreated,
        VikunjaEventTypes.LabelUpdated,
        VikunjaEventTypes.LabelDeleted,
        VikunjaEventTypes.TaskLabelCreated,
        VikunjaEventTypes.TaskLabelDeleted,
        VikunjaEventTypes.UserCreated,
        VikunjaEventTypes.TeamCreated,
        VikunjaEventTypes.TeamUpdated,
        VikunjaEventTypes.TeamDeleted,
        VikunjaEventTypes.TeamMemberAdded,
        VikunjaEventTypes.TeamMemberRemoved
    }
)));

// MCP端点
// 认证端点
app.MapPost("/mcp/auth", async (
    HttpContext context,
    IAuthenticationManager authManager,
    ILogger<Program> logger) =>
{
    try
    {
        var authRequest = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
        if (authRequest == null || !authRequest.TryGetValue("apiUrl", out var apiUrl) || !authRequest.TryGetValue("apiToken", out var apiToken))
        {
            return Results.BadRequest(new ErrorMessage("apiUrl and apiToken are required"));
        }

        var session = await authManager.AuthenticateAsync(apiUrl, apiToken);
        return Results.Ok(new AuthResponse(session.SessionId, session.AuthType.ToString()));
    }
    catch (AuthenticationException ex)
    {
        logger.LogWarning(ex, "Authentication failed");
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during authentication");
        return Results.Problem("Internal server error");
    }
});

// MCP请求处理端点
app.MapPost("/mcp/request", async (
    HttpContext context,
    IMcpServer mcpServer,
    ILogger<Program> logger) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.McpRequest);

        if (request == null)
        {
            return Results.BadRequest(new ErrorMessage("Invalid request"));
        }

        var response = await mcpServer.HandleRequestAsync(request);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling MCP request");
        return Results.Problem("Internal server error");
    }
});

// MCP服务器信息端点
app.MapGet("/mcp/info", (IMcpServer mcpServer) =>
{
    var info = mcpServer.GetServerInfo();
    return Results.Ok(info);
});

// MCP工具列表端点
app.MapGet("/mcp/tools", (IToolRegistry toolRegistry) =>
{
    var tools = toolRegistry.GetAllTools();
    var toolList = tools.Select(tool => new ToolInfo(
        tool.Name,
        tool.Description,
        tool.Subcommands
    )).ToList();

    var response = new ToolsListResponse(toolList, toolList.Count);
    return Results.Ok(response);
});

// MCP工具调用端点（RESTful风格）
app.MapPost("/mcp/tools/{toolName}/{subcommand}", async (
    string toolName,
    string subcommand,
    HttpContext context,
    IToolRegistry toolRegistry,
    IAuthenticationManager authManager,
    IResponseFactory responseFactory,
    ILogger<Program> logger) =>
{
    try
    {
        // 验证 Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Results.Unauthorized();
        }

        var authValue = authHeader.ToString();
        if (!authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var sessionId = authValue.Substring("Bearer ".Length).Trim();

        // 验证会话
        if (!authManager.IsAuthenticated(sessionId))
        {
            return Results.Unauthorized();
        }

        // 获取工具
        var tool = toolRegistry.GetTool(toolName);
        if (tool == null)
        {
            return Results.NotFound(new ErrorMessage($"Tool '{toolName}' not found"));
        }

        // 验证子命令
        if (!tool.Subcommands.Contains(subcommand))
        {
            return Results.BadRequest(new ErrorMessage($"Subcommand '{subcommand}' not found for tool '{toolName}'"));
        }

        // 读取请求体作为参数
        Dictionary<string, object?>? parameters = null;
        if (context.Request.ContentLength > 0)
        {
            parameters = await context.Request.ReadFromJsonAsync<Dictionary<string, object?>>();
        }

        parameters ??= new Dictionary<string, object?>();

        // 执行工具
        var result = await tool.ExecuteAsync(subcommand, parameters, sessionId);

        var response = new ToolExecutionResponse(true, toolName, subcommand, result);
        return Results.Ok(response);
    }
    catch (AuthenticationException ex)
    {
        logger.LogWarning(ex, "Authentication error in tool invocation");
        return Results.Unauthorized();
    }
    catch (ValidationException ex)
    {
        logger.LogWarning(ex, "Validation error in tool invocation");
        return Results.BadRequest(new ErrorMessage(ex.Message));
    }
    catch (ResourceNotFoundException ex)
    {
        logger.LogWarning(ex, "Resource not found in tool invocation");
        return Results.NotFound(new ErrorMessage(ex.Message));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in tool invocation");
        return Results.Problem("Internal server error");
    }
});

// MCP健康检查端点
app.MapGet("/mcp/health", (IMcpServer mcpServer) =>
{
    var info = mcpServer.GetServerInfo();
    var healthResponse = new McpHealthResponse(
        "healthy",
        DateTime.UtcNow,
        info.Name,
        info.Version
    );
    return Results.Ok(healthResponse);
});

// Admin API 端点
// 获取所有会话
app.MapGet("/admin/sessions", (IAuthenticationManager authManager) =>
{
    try
    {
        var sessions = authManager.GetAllSessions();
        var sessionList = sessions.Select(s => new SessionInfo(
            s.SessionId,
            s.ApiUrl,
            s.AuthType.ToString(),
            s.Created,
            s.LastAccessed,
            s.IsExpired
        )).ToList();

        return Results.Ok(new SessionsResponse(sessionList, sessionList.Count));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error getting sessions");
        return Results.Problem("Failed to get sessions");
    }
});

// 断开特定会话
app.MapDelete("/admin/sessions/{sessionId}", (
    string sessionId,
    IAuthenticationManager authManager) =>
{
    try
    {
        var success = authManager.Disconnect(sessionId);
        if (!success)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        Log.Information("Session {SessionId} disconnected via admin", sessionId);
        return Results.Ok(new MessageResponse("Session disconnected successfully"));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error disconnecting session {SessionId}", sessionId);
        return Results.Problem("Failed to disconnect session");
    }
});

// 断开所有会话
app.MapDelete("/admin/sessions", (IAuthenticationManager authManager) =>
{
    try
    {
        authManager.DisconnectAll();
        Log.Information("All sessions disconnected via admin");
        return Results.Ok(new MessageResponse("All sessions disconnected successfully"));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error disconnecting all sessions");
        return Results.Problem("Failed to disconnect all sessions");
    }
});

// 获取服务器统计
app.MapGet("/admin/stats", (
    IAuthenticationManager authManager,
    IToolRegistry toolRegistry,
    IMcpServer mcpServer) =>
{
    try
    {
        var sessions = authManager.GetAllSessions();
        var tools = toolRegistry.GetAllTools();
        var serverInfo = mcpServer.GetServerInfo();
        var process = System.Diagnostics.Process.GetCurrentProcess();

        var stats = new ServerStatsResponse(
            new ServerInfoStats(
                serverInfo.Name,
                serverInfo.Version,
                (DateTime.UtcNow - process.StartTime.ToUniversalTime()).ToString(@"hh\:mm\:ss")
            ),
            new SessionStats(
                sessions.Count,
                sessions.Count(s => !s.IsExpired)
            ),
            new ToolStats(
                tools.Count,
                tools.Sum(t => t.Subcommands.Count)
            ),
            new MemoryStats(
                process.WorkingSet64,
                process.PrivateMemorySize64
            )
        );

        return Results.Ok(stats);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error getting stats");
        return Results.Problem("Failed to get stats");
    }
});

// 执行工具（用于测试）
app.MapPost("/admin/tools/{toolName}/{subcommand}", async (
    string toolName,
    string subcommand,
    HttpContext context,
    IToolRegistry toolRegistry,
    IAuthenticationManager authManager) =>
{
    try
    {
        // 验证 X-Session-Id header
        if (!context.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader))
        {
            return Results.BadRequest(new { error = "X-Session-Id header is required" });
        }

        var sessionId = sessionIdHeader.ToString();
        if (!authManager.IsAuthenticated(sessionId))
        {
            return Results.Unauthorized();
        }

        var tool = toolRegistry.GetTool(toolName);
        if (tool == null)
        {
            return Results.NotFound(new { error = $"Tool '{toolName}' not found" });
        }

        if (!tool.Subcommands.Contains(subcommand))
        {
            return Results.BadRequest(new { error = $"Subcommand '{subcommand}' not found for tool '{toolName}'" });
        }

        Dictionary<string, object?>? parameters = null;
        if (context.Request.ContentLength > 0)
        {
            parameters = await context.Request.ReadFromJsonAsync<Dictionary<string, object?>>();
        }

        parameters ??= new Dictionary<string, object?>();
        var result = await tool.ExecuteAsync(subcommand, parameters, sessionId);

        return Results.Ok(new AdminToolExecutionResponse(true, toolName, subcommand, result));
    }
    catch (ValidationException ex)
    {
        Log.Warning(ex, "Validation error in tool execution");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error executing tool {Tool}/{Subcommand}", toolName, subcommand);
        return Results.Problem("Failed to execute tool");
    }
});

// 获取日志
app.MapGet("/admin/logs", (HttpContext context) =>
{
    try
    {
        var countStr = context.Request.Query["count"].ToString();
        var level = context.Request.Query["level"].ToString();
        var count = int.TryParse(countStr, out var c) ? c : 100;

        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logsDir))
        {
            return Results.Ok(new LogsResponse(new List<LogEntryInfo>(), 0));
        }

        var logFiles = Directory.GetFiles(logsDir, "vikunja-mcp-*.log")
            .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
            .Take(1)
            .ToList();

        if (logFiles.Count == 0)
        {
            return Results.Ok(new LogsResponse(new List<LogEntryInfo>(), 0));
        }

        var lines = System.IO.File.ReadLines(logFiles[0])
            .Reverse()
            .Take(count)
            .Reverse()
            .ToList();

        var logs = lines.Select(line =>
        {
            var parts = line.Split(new[] { ' ' }, 4);
            if (parts.Length >= 4)
            {
                return new LogEntryInfo(
                    $"{parts[0]} {parts[1]}",
                    parts[2].Trim('[', ']'),
                    parts[3]
                );
            }
            return new LogEntryInfo(
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                "INFO",
                line
            );
        }).ToList();

        if (!string.IsNullOrEmpty(level) && level != "All")
        {
            logs = logs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Results.Ok(new LogsResponse(logs, logs.Count));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error getting logs");
        return Results.Problem("Failed to get logs");
    }
});

// 清除日志
app.MapDelete("/admin/logs", () =>
{
    try
    {
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (Directory.Exists(logsDir))
        {
            foreach (var file in Directory.GetFiles(logsDir, "vikunja-mcp-*.log"))
            {
                System.IO.File.Delete(file);
            }
        }

        Log.Information("Logs cleared via admin");
        return Results.Ok(new MessageResponse("Logs cleared successfully"));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error clearing logs");
        return Results.Problem("Failed to clear logs");
    }
});

app.Run();

return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

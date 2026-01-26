using System.Text.Json;
using VikunjaHook;
using VikunjaHook.Models;
using VikunjaHook.Services;
using VikunjaHook.Mcp.Services;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Tools;
using VikunjaHook.Mcp.Middleware;

var builder = WebApplication.CreateSlimBuilder(args);

// 配置JSON序列化（支持AOT）
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 注册服务
builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();
builder.Services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddSingleton<IResponseFactory, ResponseFactory>();
builder.Services.AddSingleton<IVikunjaClientFactory, VikunjaClientFactory>();
builder.Services.AddSingleton<IMcpServer, McpServer>();
builder.Services.AddHttpClient();

// 注册MCP工具
builder.Services.AddSingleton<TasksTool>();
builder.Services.AddSingleton<ProjectsTool>();
builder.Services.AddSingleton<LabelsTool>();
builder.Services.AddSingleton<TeamsTool>();
builder.Services.AddSingleton<UsersTool>();

var app = builder.Build();

// Use exception handling middleware
app.UseExceptionHandling();

var logger = app.Logger;
logger.LogInformation("VikunjaHook MCP Server started");

// Register shutdown handler
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    logger.LogInformation("Application shutting down");
    app.Services.GetRequiredService<IAuthenticationManager>().DisconnectAll();
});

// 注册工具到MCP服务器
var mcpServer = app.Services.GetRequiredService<IMcpServer>();
mcpServer.RegisterTool(app.Services.GetRequiredService<TasksTool>());
mcpServer.RegisterTool(app.Services.GetRequiredService<ProjectsTool>());
mcpServer.RegisterTool(app.Services.GetRequiredService<LabelsTool>());
mcpServer.RegisterTool(app.Services.GetRequiredService<TeamsTool>());
mcpServer.RegisterTool(app.Services.GetRequiredService<UsersTool>());

// ===== Webhook 端点 =====
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
            logger.LogWarning("Empty webhook payload");
            return Results.BadRequest(new ErrorMessage("Invalid payload"));
        }

        if (!VikunjaEventTypes.IsValidEvent(payload.EventName))
        {
            logger.LogWarning("Unknown event: {EventName}", payload.EventName);
            return Results.BadRequest(new ErrorMessage("Unknown event type"));
        }

        await handler.HandleWebhookAsync(payload);
        return Results.Ok(new WebhookSuccessResponse("success", payload.EventName));
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "JSON parse error");
        return Results.BadRequest(new ErrorMessage("Invalid JSON"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Webhook error");
        return Results.Problem("Internal error");
    }
});

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

// ===== MCP 端点 =====
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
            return Results.BadRequest(new ErrorMessage("apiUrl and apiToken required"));
        }

        var session = await authManager.AuthenticateAsync(apiUrl, apiToken);
        return Results.Ok(new AuthResponse(session.SessionId, session.AuthType.ToString()));
    }
    catch (AuthenticationException ex)
    {
        logger.LogWarning(ex, "Auth failed");
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Auth error");
        return Results.Problem("Internal error");
    }
});

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
        logger.LogError(ex, "MCP request error");
        return Results.Problem("Internal error");
    }
});

app.MapGet("/mcp/info", (IMcpServer mcpServer) =>
{
    var info = mcpServer.GetServerInfo();
    return Results.Ok(info);
});

app.MapGet("/mcp/tools", (IToolRegistry toolRegistry) =>
{
    var tools = toolRegistry.GetAllTools();
    var toolList = tools.Select(tool => new ToolInfo(
        tool.Name,
        tool.Description,
        tool.Subcommands
    )).ToList();

    return Results.Ok(new ToolsListResponse(toolList, toolList.Count));
});

app.MapPost("/mcp/tools/{toolName}/{subcommand}", async (
    string toolName,
    string subcommand,
    HttpContext context,
    IToolRegistry toolRegistry,
    IAuthenticationManager authManager,
    ILogger<Program> logger) =>
{
    try
    {
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

        if (!authManager.IsAuthenticated(sessionId))
        {
            return Results.Unauthorized();
        }

        var tool = toolRegistry.GetTool(toolName);
        if (tool == null)
        {
            return Results.NotFound(new ErrorMessage($"Tool '{toolName}' not found"));
        }

        if (!tool.Subcommands.Contains(subcommand))
        {
            return Results.BadRequest(new ErrorMessage($"Subcommand '{subcommand}' not found"));
        }

        Dictionary<string, object?>? parameters = null;
        if (context.Request.ContentLength > 0)
        {
            parameters = await context.Request.ReadFromJsonAsync<Dictionary<string, object?>>();
        }

        parameters ??= new Dictionary<string, object?>();
        var result = await tool.ExecuteAsync(subcommand, parameters, sessionId);

        return Results.Ok(new ToolExecutionResponse(true, toolName, subcommand, result));
    }
    catch (AuthenticationException ex)
    {
        logger.LogWarning(ex, "Auth error");
        return Results.Unauthorized();
    }
    catch (ValidationException ex)
    {
        logger.LogWarning(ex, "Validation error");
        return Results.BadRequest(new ErrorMessage(ex.Message));
    }
    catch (ResourceNotFoundException ex)
    {
        logger.LogWarning(ex, "Not found");
        return Results.NotFound(new ErrorMessage(ex.Message));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Tool error");
        return Results.Problem("Internal error");
    }
});

app.MapGet("/mcp/health", (IMcpServer mcpServer) =>
{
    var info = mcpServer.GetServerInfo();
    return Results.Ok(new McpHealthResponse(
        "healthy",
        DateTime.UtcNow,
        info.Name,
        info.Version
    ));
});

// ===== Admin 端点 =====
app.MapGet("/admin/sessions", (IAuthenticationManager authManager) =>
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
});

app.MapDelete("/admin/sessions/{sessionId}", (
    string sessionId,
    IAuthenticationManager authManager,
    ILogger<Program> logger) =>
{
    var success = authManager.Disconnect(sessionId);
    if (!success)
    {
        return Results.NotFound(new { error = "Session not found" });
    }

    logger.LogInformation("Session {SessionId} disconnected", sessionId);
    return Results.Ok(new MessageResponse("Session disconnected"));
});

app.MapDelete("/admin/sessions", (IAuthenticationManager authManager, ILogger<Program> logger) =>
{
    authManager.DisconnectAll();
    logger.LogInformation("All sessions disconnected");
    return Results.Ok(new MessageResponse("All sessions disconnected"));
});

app.MapGet("/admin/stats", (
    IAuthenticationManager authManager,
    IToolRegistry toolRegistry,
    IMcpServer mcpServer) =>
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
});

app.MapPost("/admin/tools/{toolName}/{subcommand}", async (
    string toolName,
    string subcommand,
    HttpContext context,
    IToolRegistry toolRegistry,
    IAuthenticationManager authManager,
    ILogger<Program> logger) =>
{
    try
    {
        if (!context.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader))
        {
            return Results.BadRequest(new { error = "X-Session-Id header required" });
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
            return Results.BadRequest(new { error = $"Subcommand '{subcommand}' not found" });
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
        logger.LogWarning(ex, "Validation error");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Tool error");
        return Results.Problem("Internal error");
    }
});

// ===== 健康检查 =====
app.MapGet("/health", () => Results.Ok(new HealthResponse(
    "healthy",
    DateTime.UtcNow,
    "VikunjaHook"
)));

app.Run();

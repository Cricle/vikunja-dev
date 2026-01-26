using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VikunjaHook;
using VikunjaHook.Models;
using VikunjaHook.Services;
using VikunjaHook.Mcp.Services;
using VikunjaHook.Mcp.Tools;
using VikunjaHook.Mcp.Models;

// 检查是否为 MCP stdio 模式（通过环境变量或命令行参数）
var mcpMode = args.Contains("--mcp") || 
              Environment.GetEnvironmentVariable("MCP_MODE")?.ToLower() == "true";

if (mcpMode)
{
    // ===== MCP Server Mode (stdio) =====
    var builder = Host.CreateApplicationBuilder(args);

    // Configure all logs to go to stderr (stdout is used for the MCP protocol messages)
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    // Validate required environment variables
    var apiUrl = Environment.GetEnvironmentVariable("VIKUNJA_API_URL");
    var apiToken = Environment.GetEnvironmentVariable("VIKUNJA_API_TOKEN");

    if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiToken))
    {
        Console.Error.WriteLine("ERROR: VIKUNJA_API_URL and VIKUNJA_API_TOKEN environment variables are required");
        Console.Error.WriteLine("Example:");
        Console.Error.WriteLine("  VIKUNJA_API_URL=https://vikunja.example.com/api/v1");
        Console.Error.WriteLine("  VIKUNJA_API_TOKEN=your_api_token_here");
        return 1;
    }

    // Register core services
    builder.Services.AddSingleton<IVikunjaClientFactory>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = sp.GetRequiredService<ILogger<VikunjaClientFactory>>();
        return new VikunjaClientFactory(httpClientFactory, logger, apiUrl, apiToken);
    });
    builder.Services.AddHttpClient();

    // Add the MCP services: the transport to use (stdio) and the tools to register
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<TasksTools>()
        .WithTools<ProjectsTools>()
        .WithTools<LabelsTools>()
        .WithTools<TeamsTools>()
        .WithTools<UsersTools>();

    await builder.Build().RunAsync();
    return 0;
}
else
{
    // ===== Webhook API Mode (HTTP) =====
    var builder = WebApplication.CreateSlimBuilder(args);

    // 配置JSON序列化（支持AOT）
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });

    // 注册服务
    builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();
    builder.Services.AddHttpClient();

    var app = builder.Build();

    var logger = app.Logger;
    logger.LogInformation("Vikunja Webhook Server started");

    // ===== Webhook Endpoints =====

    // Webhook endpoints
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

    // Health check
    app.MapGet("/health", () => Results.Ok(new HealthResponse(
        "healthy",
        DateTime.UtcNow,
        "VikunjaHook"
    )));

    app.Run();
    return 0;
}

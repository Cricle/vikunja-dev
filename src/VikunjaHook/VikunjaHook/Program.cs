using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using VikunjaHook;
using VikunjaHook.Models;
using VikunjaHook.Services;
using VikunjaHook.Mcp.Services;
using VikunjaHook.Mcp.Tools;
using VikunjaHook.Mcp.Models;

// Validate required environment variables for MCP
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

var builder = WebApplication.CreateSlimBuilder(args);

// Configure JSON serialization for AOT compatibility
// Combine MCP SDK's JsonContext with our AppJsonSerializerContext
var jsonOptions = new JsonSerializerOptions(ModelContextProtocol.McpJsonUtilities.DefaultOptions)
{
    TypeInfoResolver = JsonTypeInfoResolver.Combine(
        AppJsonSerializerContext.Default,
        ModelContextProtocol.McpJsonUtilities.DefaultOptions.TypeInfoResolver!)
};

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Clear();
    options.SerializerOptions.TypeInfoResolverChain.Add(jsonOptions.TypeInfoResolver!);
    options.SerializerOptions.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
    options.SerializerOptions.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
    options.SerializerOptions.NumberHandling = jsonOptions.NumberHandling;
});

// Register Vikunja client factory for MCP
builder.Services.AddSingleton<IVikunjaClientFactory>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<VikunjaClientFactory>>();
    return new VikunjaClientFactory(httpClientFactory, logger, apiUrl, apiToken);
});

// Register webhook handler
builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();
builder.Services.AddHttpClient();

// Add MCP server with HTTP transport (SSE) and all tools
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<TasksTools>(jsonOptions)
    .WithTools<TaskAssigneesTools>(jsonOptions)
    .WithTools<TaskCommentsTools>(jsonOptions)
    .WithTools<TaskAttachmentsTools>(jsonOptions)
    .WithTools<TaskRelationsTools>(jsonOptions)
    .WithTools<TaskLabelsTools>(jsonOptions)
    .WithTools<ProjectsTools>(jsonOptions)
    .WithTools<LabelsTools>(jsonOptions)
    .WithTools<TeamsTools>(jsonOptions)
    .WithTools<UsersTools>(jsonOptions)
    .WithTools<BucketsTools>(jsonOptions)
    .WithTools<WebhooksTools>(jsonOptions)
    .WithTools<SavedFiltersTools>(jsonOptions);

var app = builder.Build();

app.Logger.LogInformation("Starting VikunjaHook with MCP (HTTP/SSE) + Webhook (HTTP)");

// Map MCP endpoints (HTTP with SSE transport)
app.MapMcp("/mcp");

// Map Webhook endpoints
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

app.MapGet("/health", () => Results.Ok(new HealthResponse(
    "healthy",
    DateTime.UtcNow,
    "VikunjaHook"
)));

await app.RunAsync();
return 0;

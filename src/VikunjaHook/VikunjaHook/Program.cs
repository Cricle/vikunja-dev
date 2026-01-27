using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vikunja.Core;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Mcp.Tools;
using Vikunja.Core.Models;
using Vikunja.Core.Services;
using Vikunja.Core.Notifications.Interfaces;
using Vikunja.Core.Notifications.Configuration;
using Vikunja.Core.Notifications.Templates;
using Vikunja.Core.Notifications.Providers;
using Vikunja.Core.Notifications.Routing;
using Vikunja.Core.Notifications.Adapters;
using Vikunja.Core.Notifications.Models;

// Validate required environment variables for MCP
// If not found in environment, use command line arguments
var apiUrl = Environment.GetEnvironmentVariable("VIKUNJA_API_URL");
var apiToken = Environment.GetEnvironmentVariable("VIKUNJA_API_TOKEN");

// Fallback to command line arguments if environment variables are not set
if (string.IsNullOrWhiteSpace(apiUrl) && args.Length > 0)
{
    apiUrl = args[0];
}

if (string.IsNullOrWhiteSpace(apiToken) && args.Length > 1)
{
    apiToken = args[1];
}

if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiToken))
{
    Console.Error.WriteLine("ERROR: VIKUNJA_API_URL and VIKUNJA_API_TOKEN are required");
    Console.Error.WriteLine("Provide them via:");
    Console.Error.WriteLine("  1. Environment variables:");
    Console.Error.WriteLine("     VIKUNJA_API_URL=https://vikunja.example.com/api/v1");
    Console.Error.WriteLine("     VIKUNJA_API_TOKEN=your_api_token_here");
    Console.Error.WriteLine("  2. Command line arguments:");
    Console.Error.WriteLine("     dotnet run <API_URL> <API_TOKEN>");
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

// Register notification system services
builder.Services.AddSingleton<Vikunja.Core.Notifications.Interfaces.IConfigurationManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonFileConfigurationManager>>();
    return new JsonFileConfigurationManager(logger);
});

builder.Services.AddSingleton<ITemplateEngine, SimpleTemplateEngine>();

// Register MCP Tools for the adapter
builder.Services.AddSingleton<ProjectsTools>();
builder.Services.AddSingleton<TasksTools>();
builder.Services.AddSingleton<UsersTools>();

builder.Services.AddSingleton<IMcpToolsAdapter, McpToolsAdapter>();
builder.Services.AddSingleton<IEventRouter, EventRouter>();

// Register notification providers
builder.Services.AddHttpClient<PushDeerProvider>();
builder.Services.AddSingleton<INotificationProvider, PushDeerProvider>();

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

// Serve static files from wwwroot/dist
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "dist")),
    RequestPath = ""
});

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "dist"))
});

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

// Webhook Notification System API Endpoints

// Get user configuration
app.MapGet("/api/webhook-config/{userId}", async (
    string userId,
    Vikunja.Core.Notifications.Interfaces.IConfigurationManager configManager,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Getting configuration for user {UserId}", userId);
    
    var config = await configManager.LoadUserConfigAsync(userId, cancellationToken);
    
    if (config == null)
    {
        // Return default config if not found
        config = new Vikunja.Core.Notifications.Models.UserConfig
        {
            UserId = userId,
            Providers = new List<Vikunja.Core.Notifications.Models.ProviderConfig>(),
            ProjectRules = new List<Vikunja.Core.Notifications.Models.ProjectRule>(),
            Templates = new Dictionary<string, Vikunja.Core.Notifications.Models.NotificationTemplate>(),
            LastModified = DateTime.UtcNow
        };
    }
    
    return Results.Ok(config);
});

// Update user configuration
app.MapPut("/api/webhook-config/{userId}", async (
    string userId,
    HttpContext context,
    Vikunja.Core.Notifications.Interfaces.IConfigurationManager configManager,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Updating configuration for user {UserId}", userId);
    
    var config = await context.Request.ReadFromJsonAsync(
        Vikunja.Core.Notifications.WebhookNotificationJsonContext.Default.UserConfig,
        cancellationToken);
    
    if (config == null)
    {
        return Results.BadRequest();
    }
    
    if (userId != config.UserId)
    {
        return Results.BadRequest();
    }
    
    try
    {
        await configManager.SaveUserConfigAsync(config, cancellationToken);
        return Results.Ok(config);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating configuration for user {UserId}", userId);
        return Results.Problem("Error updating configuration");
    }
});

// Test notification
app.MapPost("/api/webhook-config/{userId}/test", async (
    string userId,
    HttpContext context,
    Vikunja.Core.Notifications.Interfaces.IConfigurationManager configManager,
    IEnumerable<INotificationProvider> providers,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Testing notification for user {UserId}", userId);
    
    var request = await context.Request.ReadFromJsonAsync<TestNotificationRequest>(cancellationToken);
    
    if (request == null)
    {
        return Results.BadRequest();
    }
    
    var config = await configManager.LoadUserConfigAsync(userId, cancellationToken);
    
    if (config == null)
    {
        return Results.NotFound();
    }
    
    var provider = providers.FirstOrDefault(p => p.ProviderType == request.ProviderType);
    
    if (provider == null)
    {
        return Results.BadRequest();
    }
    
    var providerConfig = config.Providers.FirstOrDefault(p => p.ProviderType == request.ProviderType);
    
    if (providerConfig == null)
    {
        return Results.BadRequest();
    }
    
    try
    {
        var message = new Vikunja.Core.Notifications.Models.NotificationMessage(
            Title: request.Title ?? "Test Notification",
            Body: request.Body ?? "This is a test notification from Vikunja Webhook System",
            Format: Vikunja.Core.Notifications.Models.NotificationFormat.Text);
        
        Vikunja.Core.Notifications.Models.NotificationResult result;
        
        // Special handling for PushDeer
        if (provider is PushDeerProvider pushDeer &&
            providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
        {
            result = await pushDeer.SendAsync(message, pushKey, cancellationToken);
        }
        else
        {
            result = await provider.SendAsync(message, cancellationToken);
        }
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error testing notification for user {UserId}", userId);
        return Results.Problem("Error testing notification");
    }
});

// Receive webhook from Vikunja
app.MapPost("/api/webhook", async (
    HttpContext context,
    IEventRouter eventRouter,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var webhookEvent = await context.Request.ReadFromJsonAsync(
        Vikunja.Core.Notifications.WebhookNotificationJsonContext.Default.WebhookEvent,
        cancellationToken);
    
    if (webhookEvent == null)
    {
        return Results.BadRequest();
    }
    
    logger.LogInformation("Received webhook event: {EventType} for project {ProjectId}",
        webhookEvent.EventType, webhookEvent.ProjectId);
    
    try
    {
        // Route event asynchronously (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await eventRouter.RouteEventAsync(webhookEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error routing webhook event");
            }
        }, cancellationToken);
        
        return Results.Accepted();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error receiving webhook");
        return Results.Problem("Error processing webhook");
    }
});

// Vikunja API proxy endpoints
app.MapGet("/api/vikunja/projects", async (
    IVikunjaClientFactory clientFactory,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var client = clientFactory.GetClient();
        var response = await client.GetAsync("projects", cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Results.Content(content, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching Vikunja projects");
        return Results.Problem("Error fetching projects");
    }
});

app.MapGet("/api/vikunja/projects/{id:int}", async (
    int id,
    IVikunjaClientFactory clientFactory,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var client = clientFactory.GetClient();
        var response = await client.GetAsync($"projects/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Results.Content(content, "application/json");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching Vikunja project {ProjectId}", id);
        return Results.Problem("Error fetching project");
    }
});

await app.RunAsync();
return 0;

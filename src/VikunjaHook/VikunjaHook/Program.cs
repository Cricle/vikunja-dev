using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vikunja.Core;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Mcp.Tools;
using Vikunja.Core.Models;
using Vikunja.Core.Services;
using Vikunja.Core.Notifications;
using Vikunja.Core.Notifications.Providers;
using Vikunja.Core.Notifications.Models;

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

// Get API URL and Token from configuration (supports environment variables, command line args, etc.)
var apiUrl = builder.Configuration["VIKUNJA_API_URL"];
var apiToken = builder.Configuration["VIKUNJA_API_TOKEN"];

if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiToken))
{
    Console.Error.WriteLine("ERROR: VIKUNJA_API_URL and VIKUNJA_API_TOKEN are required");
    Console.Error.WriteLine("Provide them via:");
    Console.Error.WriteLine("  1. Environment variables:");
    Console.Error.WriteLine("     VIKUNJA_API_URL=https://vikunja.example.com/api/v1");
    Console.Error.WriteLine("     VIKUNJA_API_TOKEN=your_api_token_here");
    Console.Error.WriteLine("  2. Command line arguments:");
    Console.Error.WriteLine("     dotnet run --VIKUNJA_API_URL=... --VIKUNJA_API_TOKEN=...");
    Console.Error.WriteLine("  3. Docker environment variables:");
    Console.Error.WriteLine("     docker run -e VIKUNJA_API_URL=... -e VIKUNJA_API_TOKEN=...");
    return 1;
}

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
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonFileConfigurationManager>>();
    return new JsonFileConfigurationManager(logger);
});

builder.Services.AddSingleton<SimpleTemplateEngine>();

// Register push event history (lock-free, keeps last 30 records)
builder.Services.AddSingleton(new InMemoryPushEventHistory(maxRecords: 30));

// Register task reminder history (keeps last 100 records)
builder.Services.AddSingleton(new TaskReminderHistory(maxRecords: 100));

// Register MCP Tools for the adapter
builder.Services.AddSingleton<ProjectsTools>();
builder.Services.AddSingleton<TasksTools>();
builder.Services.AddSingleton<UsersTools>();

// Register McpToolsAdapter with Vikunja URL
builder.Services.AddSingleton(sp =>
{
    var projectsTools = sp.GetRequiredService<ProjectsTools>();
    var tasksTools = sp.GetRequiredService<TasksTools>();
    var usersTools = sp.GetRequiredService<UsersTools>();
    var logger = sp.GetRequiredService<ILogger<McpToolsAdapter>>();
    var vikunjaUrl = builder.Configuration["VIKUNJA_URL"];
    
    return new McpToolsAdapter(projectsTools, tasksTools, usersTools, logger, vikunjaUrl);
});

// Register EventRouter with Vikunja URL from configuration
builder.Services.AddSingleton(sp =>
{
    var configManager = sp.GetRequiredService<JsonFileConfigurationManager>();
    var templateEngine = sp.GetRequiredService<SimpleTemplateEngine>();
    var clientFactory = sp.GetRequiredService<IVikunjaClientFactory>();
    var mcpTools = sp.GetRequiredService<McpToolsAdapter>();
    var providers = sp.GetServices<PushDeerProvider>();
    var pushHistory = sp.GetRequiredService<InMemoryPushEventHistory>();
    var reminderService = sp.GetRequiredService<TaskReminderService>();
    var logger = sp.GetRequiredService<ILogger<EventRouter>>();
    
    // Get Vikunja URL from configuration (environment variable or appsettings)
    var vikunjaUrl = builder.Configuration["VIKUNJA_URL"];
    
    return new EventRouter(
        clientFactory,
        configManager,
        templateEngine,
        mcpTools,
        providers,
        pushHistory,
        reminderService,
        logger,
        vikunjaUrl);
});

// Register notification providers
builder.Services.AddHttpClient<PushDeerProvider>();
builder.Services.AddSingleton<PushDeerProvider>();

// Register TaskReminderService
builder.Services.AddSingleton(sp =>
{
    var clientFactory = sp.GetRequiredService<IVikunjaClientFactory>();
    var configManager = sp.GetRequiredService<JsonFileConfigurationManager>();
    var templateEngine = sp.GetRequiredService<SimpleTemplateEngine>();
    var providers = sp.GetServices<PushDeerProvider>();
    var pushHistory = sp.GetRequiredService<InMemoryPushEventHistory>();
    var reminderHistory = sp.GetRequiredService<TaskReminderHistory>();
    var logger = sp.GetRequiredService<ILogger<TaskReminderService>>();
    var vikunjaUrl = builder.Configuration["VIKUNJA_URL"];
    
    return new TaskReminderService(
        clientFactory,
        configManager,
        templateEngine,
        providers,
        pushHistory,
        reminderHistory,
        logger,
        vikunjaUrl);
});

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

// Start TaskReminderService
var reminderService = app.Services.GetRequiredService<TaskReminderService>();
reminderService.Start();
app.Logger.LogInformation("TaskReminderService started");

// Serve static files from wwwroot/dist
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "dist")),
    RequestPath = ""
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
    JsonFileConfigurationManager configManager,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Getting configuration for user {UserId}", userId);
    
    var config = await configManager.LoadUserConfigAsync(userId, cancellationToken);
    
    if (config == null)
    {
        // Return default config if not found
        config = new UserConfig
        {
            UserId = userId,
            Providers = new List<ProviderConfig>(),
            DefaultProviders = new List<string>(),
            Templates = new Dictionary<string, NotificationTemplate>(),
            LastModified = DateTime.UtcNow
        };
    }
    
    return Results.Ok(config);
});

// Update user configuration
app.MapPut("/api/webhook-config/{userId}", async (
    string userId,
    HttpContext context,
    JsonFileConfigurationManager configManager,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Updating configuration for user {UserId}", userId);
    
    var config = await context.Request.ReadFromJsonAsync(
        WebhookNotificationJsonContext.Default.UserConfig,
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
    JsonFileConfigurationManager configManager,
    IEnumerable<PushDeerProvider> providers,
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
        var message = new NotificationMessage(
            Title: request.Title ?? "Test Notification",
            Body: request.Body ?? "This is a test notification from Vikunja Webhook System",
            Format: NotificationFormat.Text);
        
        NotificationResult result;
        
        // Special handling for PushDeer
        if (provider is PushDeerProvider pushDeer)
        {
            if (providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
            {
                result = await pushDeer.SendAsync(message, pushKey, cancellationToken);
            }
            else
            {
                return Results.BadRequest();
            }
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
    EventRouter eventRouter,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    // Read the raw JSON first for logging
    context.Request.EnableBuffering();
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    var rawJson = await reader.ReadToEndAsync(cancellationToken);
    context.Request.Body.Position = 0;
    
    logger.LogInformation("Received webhook raw JSON: {RawJson}", rawJson);
    
    var webhookEvent = await context.Request.ReadFromJsonAsync(
        WebhookNotificationJsonContext.Default.WebhookEvent,
        cancellationToken);
    
    if (webhookEvent == null)
    {
        logger.LogWarning("Received null webhook event");
        return Results.BadRequest();
    }
    
    logger.LogInformation("Received webhook event: {EventName} at {Time}",
        webhookEvent.EventName, webhookEvent.Time);
    logger.LogInformation("Webhook data: {Data}", webhookEvent.Data.ToString());
    
    // Log parsed task data if available
    if (webhookEvent.Task != null)
    {
        logger.LogInformation("Parsed task data: Id={TaskId}, Title={Title}, Done={Done}", 
            webhookEvent.Task.Id, webhookEvent.Task.Title, webhookEvent.Task.Done);
    }
    
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

// Get push event history
app.MapGet("/api/push-history", (
    InMemoryPushEventHistory pushHistory,
    int? count) =>
{
    var records = pushHistory.GetRecentRecords(count ?? 50);
    var response = new PushHistoryResponse
    {
        Records = records,
        TotalCount = pushHistory.GetTotalCount()
    };
    return Results.Ok(response);
});

// Clear push event history
app.MapDelete("/api/push-history", (InMemoryPushEventHistory pushHistory) =>
{
    pushHistory.Clear();
    var response = new ClearHistoryResponse { Message = "History cleared" };
    return Results.Ok(response);
});

// Get task reminder history
app.MapGet("/api/reminder-history", (
    TaskReminderHistory reminderHistory,
    int? count) =>
{
    var records = reminderHistory.GetRecentRecords(count ?? 50);
    return new ReminderHistoryResponse
    {
        Records = records,
        TotalCount = reminderHistory.GetTotalCount()
    };
});

// Clear task reminder history
app.MapDelete("/api/reminder-history", (TaskReminderHistory reminderHistory) =>
{
    reminderHistory.Clear();
    return new ReminderClearResponse { Message = "Reminder history cleared" };
});

// Add test reminder data (for development/testing)
app.MapPost("/api/reminder-history/test", (TaskReminderHistory reminderHistory) =>
{
    var random = new Random();
    var reminderTypes = new[] { "due", "start", "reminder" };
    var projects = new[] { "Personal Tasks", "Work Project", "Home Improvement", "Learning Goals" };
    var tasks = new[] 
    { 
        "Complete project documentation",
        "Review pull requests",
        "Prepare presentation slides",
        "Update dependencies",
        "Fix critical bug",
        "Write unit tests",
        "Deploy to production",
        "Team meeting preparation"
    };
    
    for (int i = 0; i < 10; i++)
    {
        var taskTitle = tasks[random.Next(tasks.Length)];
        var projectTitle = projects[random.Next(projects.Length)];
        var reminderType = reminderTypes[random.Next(reminderTypes.Length)];
        var success = random.Next(100) > 10; // 90% success rate
        
        reminderHistory.AddRecord(new TaskReminderRecord
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 120)),
            TaskId = random.Next(1, 1000),
            TaskTitle = taskTitle,
            ProjectTitle = projectTitle,
            ReminderType = reminderType,
            UserId = "testuser",
            Title = $"⏰ Task Reminder: {taskTitle}",
            Body = $"Task: {taskTitle}\nProject: {projectTitle}\nType: {reminderType}",
            Providers = new List<string> { "PushDeer" },
            Success = success,
            ErrorMessage = success ? null : "Failed to send notification"
        });
    }
    
    return new ReminderTestResponse { Message = "Test data added", Count = 10 };
});

// Get all labels from Vikunja
app.MapGet("/api/mcp/labels", async (
    IVikunjaClientFactory clientFactory,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var labels = await clientFactory.GetAsync<List<VikunjaLabel>>("labels", cancellationToken);
        return Results.Ok(labels ?? new List<VikunjaLabel>());
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get labels");
        return Results.Ok(new List<VikunjaLabel>());
    }
});

// Get reminder status (for monitoring)
app.MapGet("/api/reminder-status", (TaskReminderService reminderService) =>
{
    var status = reminderService.GetReminderStatus();
    return Results.Ok(status);
});

// SPA fallback - must be last to not interfere with API routes
// Only fallback for non-API routes
app.MapFallback(async context =>
{
    // Don't fallback for API routes
    if (context.Request.Path.StartsWithSegments("/api") || 
        context.Request.Path.StartsWithSegments("/mcp") ||
        context.Request.Path.StartsWithSegments("/webhook") ||
        context.Request.Path.StartsWithSegments("/health"))
    {
        context.Response.StatusCode = 404;
        return;
    }
    
    // Serve index.html for SPA routes
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot", "dist"));
    var fileInfo = fileProvider.GetFileInfo("index.html");
    
    if (fileInfo.Exists)
    {
        context.Response.ContentType = "text/html";
        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

await app.RunAsync();
return 0;

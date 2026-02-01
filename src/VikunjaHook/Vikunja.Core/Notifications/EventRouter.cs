using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Models;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Providers;

namespace Vikunja.Core.Notifications;

public class EventRouter
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly JsonFileConfigurationManager _configManager;
    private readonly SimpleTemplateEngine _templateEngine;
    private readonly McpToolsAdapter _mcpTools;
    private readonly IEnumerable<NotificationProviderBase> _providers;
    private readonly InMemoryPushEventHistory _pushHistory;
    private readonly ILogger<EventRouter> _logger;
    private readonly string? _vikunjaUrl;

    public EventRouter(
        IVikunjaClientFactory clientFactory,
        JsonFileConfigurationManager configManager,
        SimpleTemplateEngine templateEngine,
        McpToolsAdapter mcpTools,
        IEnumerable<NotificationProviderBase> providers,
        InMemoryPushEventHistory pushHistory,
        ILogger<EventRouter> logger,
        string? vikunjaUrl = null)
    {
        _clientFactory = clientFactory;
        _configManager = configManager;
        _templateEngine = templateEngine;
        _mcpTools = mcpTools;
        _providers = providers;
        _pushHistory = pushHistory;
        _logger = logger;
        _vikunjaUrl = vikunjaUrl?.TrimEnd('/');
    }

    public async Task RouteEventAsync(
        WebhookEvent webhookEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Routing webhook event: {EventType} for project {ProjectId}",
            webhookEvent.EventType, webhookEvent.ProjectId);

        // Load all user configurations
        var configs = await _configManager.LoadAllConfigsAsync(cancellationToken);

        if (configs.Count == 0)
        {
            _logger.LogWarning("No user configurations found");
            return;
        }

        // Process each user configuration asynchronously
        var tasks = configs.Select(config => ProcessUserConfigAsync(config, webhookEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    private async Task ProcessUserConfigAsync(
        UserConfig config,
        WebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing webhook event {EventType} for user {UserId}",
                webhookEvent.EventType, config.UserId);

            // Get template for this event type
            var template = GetTemplate(config, webhookEvent.EventType);

            // Check if this is a task.updated event with OnlyNotifyWhenCompleted enabled
            if (webhookEvent.EventType == VikunjaEventTypes.TaskUpdated && template.OnlyNotifyWhenCompleted)
            {
                var taskDone = webhookEvent.Task?.Done ?? false;
                var taskId = webhookEvent.Task?.Id ?? 0;
                var taskTitle = webhookEvent.Task?.Title ?? "";
                
                _logger.LogWarning("[OnlyNotifyWhenCompleted] User={UserId}, TaskId={TaskId}, Title={Title}, Done={Done}, OnlyNotifyWhenCompleted={OnlyNotify}",
                    config.UserId, taskId, taskTitle, taskDone, template.OnlyNotifyWhenCompleted);
                
                // Only proceed if the task is marked as done in the webhook data
                if (webhookEvent.Task == null || !webhookEvent.Task.Done)
                {
                    _logger.LogInformation("Skipping task.updated notification for user {UserId} - task not completed (OnlyNotifyWhenCompleted=true, done={Done})",
                        config.UserId, webhookEvent.Task?.Done ?? false);
                    return;
                }
                
                _logger.LogInformation("Task completed - sending notification for user {UserId}", config.UserId);
            }

            // Enrich event data using MCP tools
            var context = await EnrichEventDataAsync(webhookEvent, cancellationToken);

            // Render template
            var title = _templateEngine.Render(template.Title, context);
            var body = _templateEngine.Render(template.Body, context);

            // Send notifications to configured providers (template-specific or default)
            await SendNotificationsAsync(config, title, body, template.Format, template, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event for user {UserId}", config.UserId);
        }
    }

    private async Task<TemplateContext> EnrichEventDataAsync(
        WebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var eventUrl = GenerateEventUrl(webhookEvent);
        
        var context = new TemplateContext
        {
            Event = new EventData
            {
                Type = webhookEvent.EventType,
                Timestamp = webhookEvent.Timestamp,
                Url = eventUrl
            }
        };

        // Enrich with task data - use webhook data first, then try API
        if (webhookEvent.Task != null && webhookEvent.Task.Id > 0)
        {
            // Start with webhook data
            var taskData = new TaskTemplateData
            {
                Id = webhookEvent.Task.Id,
                Title = webhookEvent.Task.Title,
                Description = webhookEvent.Task.Description,
                Done = webhookEvent.Task.Done,
                Url = string.IsNullOrWhiteSpace(_vikunjaUrl) ? string.Empty : $"{_vikunjaUrl}/tasks/{webhookEvent.Task.Id}"
            };
            
            // Try to enrich with API data (may fail if token is invalid)
            try
            {
                var apiTaskData = await _mcpTools.GetTaskAsync(webhookEvent.Task.Id, cancellationToken);
                if (apiTaskData != null)
                {
                    // Merge API data with webhook data
                    taskData.DueDate = apiTaskData.DueDate;
                    taskData.Priority = apiTaskData.Priority;
                }
                
                var assignees = await _mcpTools.GetTaskAssigneesAsync(webhookEvent.Task.Id, cancellationToken);
                var labels = await _mcpTools.GetTaskLabelsAsync(webhookEvent.Task.Id, cancellationToken);
                
                context = context with 
                { 
                    Task = taskData,
                    Assignees = assignees,
                    Labels = labels
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich task data from API, using webhook data only");
                context = context with { Task = taskData };
            }
        }

        // Enrich with project data - try API, fallback to basic data from webhook
        if (webhookEvent.ProjectId > 0)
        {
            var projectData = await _mcpTools.GetProjectAsync(webhookEvent.ProjectId, cancellationToken);
            if (projectData != null)
            {
                context = context with { Project = projectData };
            }
            else
            {
                // API failed, create basic project data with URL
                _logger.LogWarning("Failed to get project data from API for project {ProjectId}, using basic data", webhookEvent.ProjectId);
                context = context with 
                { 
                    Project = new ProjectTemplateData 
                    { 
                        Id = webhookEvent.ProjectId,
                        Title = $"Project #{webhookEvent.ProjectId}",
                        Url = string.IsNullOrWhiteSpace(_vikunjaUrl) ? string.Empty : $"{_vikunjaUrl}/projects/{webhookEvent.ProjectId}"
                    } 
                };
            }
        }

        // Extract comment data from webhook event
        if (webhookEvent.EventType.Contains("comment") && 
            webhookEvent.Data.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (webhookEvent.Data.TryGetProperty("id", out var commentId) &&
                webhookEvent.Data.TryGetProperty("comment", out var commentText))
            {
                var comment = new CommentTemplateData
                {
                    Id = commentId.GetInt32(),
                    Text = commentText.GetString() ?? string.Empty
                };
                
                // Try to get author info
                if (webhookEvent.Data.TryGetProperty("author", out var authorProp) &&
                    authorProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (authorProp.TryGetProperty("username", out var username))
                    {
                        comment.Author = username.GetString() ?? string.Empty;
                    }
                }
                
                context = context with { Comment = comment };
                
                // Try to get task info from task_id in comment event
                if (webhookEvent.Data.TryGetProperty("task_id", out var commentTaskId) && commentTaskId.GetInt32() > 0)
                {
                    context = await EnrichTaskDataAsync(context, commentTaskId.GetInt32(), cancellationToken);
                }
            }
        }

        // Extract attachment data from webhook event
        if (webhookEvent.EventType.Contains("attachment") &&
            webhookEvent.Data.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (webhookEvent.Data.TryGetProperty("id", out var attachmentId) &&
                webhookEvent.Data.TryGetProperty("file_name", out var fileName))
            {
                var attachment = new AttachmentTemplateData
                {
                    Id = attachmentId.GetInt32(),
                    FileName = fileName.GetString() ?? string.Empty
                };
                
                context = context with { Attachment = attachment };
                
                // Try to get task info from task_id in attachment event
                if (webhookEvent.Data.TryGetProperty("task_id", out var attachmentTaskId) && attachmentTaskId.GetInt32() > 0)
                {
                    context = await EnrichTaskDataAsync(context, attachmentTaskId.GetInt32(), cancellationToken);
                }
            }
        }

        // Extract relation data from webhook event
        if (webhookEvent.EventType.Contains("relation") &&
            webhookEvent.Data.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (webhookEvent.Data.TryGetProperty("task_id", out var taskId) &&
                webhookEvent.Data.TryGetProperty("other_task_id", out var otherTaskId))
            {
                var relation = new RelationTemplateData
                {
                    TaskId = taskId.GetInt32(),
                    RelatedTaskId = otherTaskId.GetInt32()
                };
                
                if (webhookEvent.Data.TryGetProperty("relation_kind", out var relationType))
                {
                    relation.RelationType = relationType.GetString() ?? string.Empty;
                }
                
                context = context with { Relation = relation };
                
                // Try to get task info from task_id in relation event
                if (taskId.GetInt32() > 0)
                {
                    context = await EnrichTaskDataAsync(context, taskId.GetInt32(), cancellationToken);
                }
            }
        }

        // Try to extract user data from webhook event data
        if (webhookEvent.Data.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            // Try to get user_id or userId
            if (webhookEvent.Data.TryGetProperty("user_id", out var userIdProp) ||
                webhookEvent.Data.TryGetProperty("userId", out userIdProp))
            {
                if (userIdProp.TryGetInt32(out var userId) && userId > 0)
                {
                    var userData = await _mcpTools.GetUserAsync(userId, cancellationToken);
                    context = context with { User = userData };
                }
            }
            
            // Try to get username directly from event data
            if (context.User == null && 
                webhookEvent.Data.TryGetProperty("username", out var usernameProp))
            {
                var username = usernameProp.GetString();
                if (!string.IsNullOrEmpty(username))
                {
                    context = context with 
                    { 
                        User = new UserTemplateData 
                        { 
                            Username = username,
                            Name = username 
                        } 
                    };
                }
            }
        }

        return context;
    }

    private string GenerateEventUrl(WebhookEvent webhookEvent)
    {
        if (string.IsNullOrWhiteSpace(_vikunjaUrl))
        {
            return string.Empty;
        }

        // Generate URL based on event type and data
        if (webhookEvent.Task != null)
        {
            // Task-related events: link to task detail
            return $"{_vikunjaUrl}/tasks/{webhookEvent.Task.Id}";
        }
        else if (webhookEvent.ProjectId > 0)
        {
            // Project-related events: link to project
            return $"{_vikunjaUrl}/projects/{webhookEvent.ProjectId}";
        }

        // Default: link to home
        return _vikunjaUrl;
    }

    private NotificationTemplate GetTemplate(UserConfig config, string eventType)
    {
        // Try to get user-defined template
        if (config.Templates.TryGetValue(eventType, out var template))
        {
            return template;
        }

        // Fall back to default template
        if (DefaultTemplates.Templates.TryGetValue(eventType, out var defaultTemplate))
        {
            return defaultTemplate;
        }

        // Ultimate fallback
        return new NotificationTemplate
        {
            EventType = eventType,
            Title = "{{event.type}}",
            Body = "Event occurred at {{event.timestamp}}",
            Format = NotificationFormat.Text
        };
    }

    private async Task SendNotificationsAsync(
        UserConfig config,
        string title,
        string body,
        NotificationFormat format,
        NotificationTemplate template,
        CancellationToken cancellationToken)
    {
        var message = new NotificationMessage(title, body, format);

        // Determine which providers to use
        List<ProviderConfig> providersToUse;
        
        if (template.Providers.Count > 0)
        {
            // Use template-specific providers
            providersToUse = config.Providers
                .Where(p => template.Providers.Contains(p.ProviderType))
                .ToList();
            
            if (providersToUse.Count == 0)
            {
                _logger.LogWarning("Template specifies providers {Providers} but none are configured for user {UserId}",
                    string.Join(", ", template.Providers), config.UserId);
                return;
            }
        }
        else if (config.DefaultProviders.Count > 0)
        {
            // Use default providers
            providersToUse = config.Providers
                .Where(p => config.DefaultProviders.Contains(p.ProviderType))
                .ToList();
            
            if (providersToUse.Count == 0)
            {
                _logger.LogWarning("Default providers {Providers} specified but none are configured for user {UserId}",
                    string.Join(", ", config.DefaultProviders), config.UserId);
                return;
            }
        }
        else
        {
            // Use all configured providers
            providersToUse = config.Providers.ToList();
        }

        if (providersToUse.Count == 0)
        {
            _logger.LogWarning("No providers configured for user {UserId}", config.UserId);
            return;
        }

        _logger.LogInformation("Sending notification to {Count} provider(s): {Providers}",
            providersToUse.Count, string.Join(", ", providersToUse.Select(p => p.ProviderType)));

        var providerResults = new List<ProviderPushResult>();

        foreach (var providerConfig in providersToUse)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderType == providerConfig.ProviderType);

            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderType} not found", providerConfig.ProviderType);
                providerResults.Add(new ProviderPushResult
                {
                    ProviderType = providerConfig.ProviderType,
                    Success = false,
                    Message = "Provider not found",
                    Timestamp = DateTime.UtcNow,
                    NotificationContent = message
                });
                continue;
            }

            try
            {
                var result = await NotificationHelper.SendNotificationAsync(
                    provider,
                    providerConfig,
                    message,
                    cancellationToken
                );

                providerResults.Add(new ProviderPushResult
                {
                    ProviderType = providerConfig.ProviderType,
                    Success = result.Success,
                    Message = result.Success ? "Sent successfully" : result.ErrorMessage,
                    Timestamp = DateTime.UtcNow,
                    NotificationContent = message
                });

                if (result.Success)
                {
                    _logger.LogInformation("Notification sent successfully via {ProviderType}",
                        providerConfig.ProviderType);
                }
                else
                {
                    _logger.LogWarning("Failed to send notification via {ProviderType}: {Error}",
                        providerConfig.ProviderType, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification via {ProviderType}",
                    providerConfig.ProviderType);
                
                providerResults.Add(new ProviderPushResult
                {
                    ProviderType = providerConfig.ProviderType,
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    NotificationContent = message
                });
            }
        }

        // Record push event
        var record = new PushEventRecord
        {
            Id = Guid.NewGuid().ToString(),
            EventName = template.EventType,
            Timestamp = DateTime.UtcNow,
            EventData = new EventDataInfo 
            { 
                Title = title, 
                Body = body, 
                Format = format.ToString() 
            },
            Providers = providerResults
        };
        
        _pushHistory.AddRecord(record);
    }

    // 辅助方法：获取任务数据并丰富上下文
    private async Task<TemplateContext> EnrichTaskDataAsync(TemplateContext context, int taskId, CancellationToken cancellationToken)
    {
        try
        {
            var taskData = await _mcpTools.GetTaskAsync(taskId, cancellationToken);
            if (taskData != null)
            {
                return context with { Task = taskData };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get task data for task {TaskId}", taskId);
        }
        
        // Create basic task data as fallback
        return context with 
        { 
            Task = new TaskTemplateData 
            { 
                Id = taskId,
                Url = string.IsNullOrWhiteSpace(_vikunjaUrl) ? string.Empty : $"{_vikunjaUrl}/tasks/{taskId}"
            } 
        };
    }
}

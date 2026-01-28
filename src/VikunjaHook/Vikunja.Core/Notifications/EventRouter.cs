using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Providers;

namespace Vikunja.Core.Notifications;

public class EventRouter
{
    private readonly JsonFileConfigurationManager _configManager;
    private readonly SimpleTemplateEngine _templateEngine;
    private readonly McpToolsAdapter _mcpTools;
    private readonly IEnumerable<PushDeerProvider> _providers;
    private readonly InMemoryPushEventHistory _pushHistory;
    private readonly ILogger<EventRouter> _logger;
    private readonly string? _vikunjaUrl;

    public EventRouter(
        JsonFileConfigurationManager configManager,
        SimpleTemplateEngine templateEngine,
        McpToolsAdapter mcpTools,
        IEnumerable<PushDeerProvider> providers,
        InMemoryPushEventHistory pushHistory,
        ILogger<EventRouter> logger,
        string? vikunjaUrl = null)
    {
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
            // No filtering - send all events
            _logger.LogInformation("Processing webhook event {EventType} for user {UserId}",
                webhookEvent.EventType, config.UserId);

            // Enrich event data using MCP tools
            var context = await EnrichEventDataAsync(webhookEvent, cancellationToken);

            // Get template for this event type
            var template = GetTemplate(config, webhookEvent.EventType);

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

        // Enrich with project data
        if (webhookEvent.ProjectId > 0)
        {
            var projectData = await _mcpTools.GetProjectAsync(webhookEvent.ProjectId, cancellationToken);
            context = new TemplateContext
            {
                Event = context.Event,
                Project = projectData,
                Task = context.Task,
                User = context.User,
                Assignees = context.Assignees,
                Labels = context.Labels
            };
        }

        // Enrich with task data
        if (webhookEvent.Task != null)
        {
            var taskData = await _mcpTools.GetTaskAsync(webhookEvent.Task.Id, cancellationToken);
            
            if (taskData != null)
            {
                var assignees = await _mcpTools.GetTaskAssigneesAsync(webhookEvent.Task.Id, cancellationToken);
                var labels = await _mcpTools.GetTaskLabelsAsync(webhookEvent.Task.Id, cancellationToken);
                
                context = new TemplateContext
                {
                    Event = context.Event,
                    Project = context.Project,
                    Task = taskData,
                    User = context.User,
                    Assignees = assignees,
                    Labels = labels
                };
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
                NotificationResult result;
                
                // For PushDeer, we need to pass the API key
                if (provider is Providers.PushDeerProvider pushDeerProvider &&
                    providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
                {
                    result = await pushDeerProvider.SendAsync(message, pushKey, cancellationToken);
                }
                else
                {
                    result = await provider.SendAsync(message, cancellationToken);
                }

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
}

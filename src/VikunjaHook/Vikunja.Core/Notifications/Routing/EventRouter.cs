using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Interfaces;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Templates;

namespace Vikunja.Core.Notifications.Routing;

public class EventRouter : IEventRouter
{
    private readonly IConfigurationManager _configManager;
    private readonly ITemplateEngine _templateEngine;
    private readonly IMcpToolsAdapter _mcpTools;
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger<EventRouter> _logger;

    public EventRouter(
        IConfigurationManager configManager,
        ITemplateEngine templateEngine,
        IMcpToolsAdapter mcpTools,
        IEnumerable<INotificationProvider> providers,
        ILogger<EventRouter> logger)
    {
        _configManager = configManager;
        _templateEngine = templateEngine;
        _mcpTools = mcpTools;
        _providers = providers;
        _logger = logger;
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
        var context = new TemplateContext
        {
            Event = new EventData
            {
                Type = webhookEvent.EventType,
                Timestamp = webhookEvent.Timestamp,
                Url = string.Empty // TODO: Generate URL based on Vikunja instance
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

        foreach (var providerConfig in providersToUse)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderType == providerConfig.ProviderType);

            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderType} not found", providerConfig.ProviderType);
                continue;
            }

            try
            {
                // For PushDeer, we need to pass the API key
                if (provider is Providers.PushDeerProvider pushDeerProvider &&
                    providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
                {
                    var result = await pushDeerProvider.SendAsync(message, pushKey, cancellationToken);

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
                else
                {
                    var result = await provider.SendAsync(message, cancellationToken);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification via {ProviderType}",
                    providerConfig.ProviderType);
            }
        }
    }
}

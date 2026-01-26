using Microsoft.Extensions.Logging;
using VikunjaHook.Models;

namespace VikunjaHook.Services;

/// <summary>
/// Example custom webhook handler showing how to override specific event methods.
/// To use this handler, register it in Program.cs:
/// builder.Services.AddSingleton&lt;IWebhookHandler, CustomWebhookHandlerExample&gt;();
/// </summary>
public class CustomWebhookHandlerExample : WebhookHandlerBase
{
    public CustomWebhookHandlerExample(ILogger<CustomWebhookHandlerExample> logger) : base(logger)
    {
    }

    // Override only the events you care about

    protected override async Task OnTaskCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Custom handler: New task created!");
        
        // Add your custom logic here
        // For example:
        // - Send notification to Slack/Discord
        // - Update external database
        // - Trigger automation workflow
        // - Send email notification
        
        await base.OnTaskCreatedAsync(payload, cancellationToken);
    }

    protected override async Task OnTaskUpdatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Custom handler: Task updated!");
        
        // Add your custom logic here
        
        await base.OnTaskUpdatedAsync(payload, cancellationToken);
    }

    protected override async Task OnTaskCommentCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Custom handler: New comment on task!");
        
        // Add your custom logic here
        // For example:
        // - Notify task assignees
        // - Parse @mentions and send notifications
        // - Archive comment to external system
        
        await base.OnTaskCommentCreatedAsync(payload, cancellationToken);
    }

    protected override async Task OnProjectCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Custom handler: New project created!");
        
        // Add your custom logic here
        // For example:
        // - Create default labels
        // - Set up default buckets
        // - Initialize project templates
        // - Send welcome notification
        
        await base.OnProjectCreatedAsync(payload, cancellationToken);
    }

    protected override async Task OnTeamMemberAddedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Custom handler: Team member added!");
        
        // Add your custom logic here
        // For example:
        // - Send welcome email to new member
        // - Grant access to related resources
        // - Update external user management system
        
        await base.OnTeamMemberAddedAsync(payload, cancellationToken);
    }

    protected override async Task OnErrorAsync(VikunjaWebhookPayload payload, Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(exception, "Custom handler: Error processing webhook!");
        
        // Add your custom error handling here
        // For example:
        // - Send alert to monitoring system
        // - Store failed event for retry
        // - Send notification to admin
        
        await base.OnErrorAsync(payload, exception, cancellationToken);
    }
}

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public partial class SimpleTemplateEngine : ITemplateEngine
{
    private readonly ILogger<SimpleTemplateEngine> _logger;
    
    // AOT-compatible compiled regex
    [GeneratedRegex(@"\{\{([a-zA-Z0-9_.]+)\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    public SimpleTemplateEngine(ILogger<SimpleTemplateEngine> logger)
    {
        _logger = logger;
    }

    public string Render(string template, TemplateContext context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        try
        {
            return PlaceholderRegex().Replace(template, match =>
            {
                var placeholder = match.Groups[1].Value;
                var value = GetPlaceholderValue(placeholder, context);
                return value ?? string.Empty;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template");
            return template;
        }
    }

    public IReadOnlyList<string> GetAvailablePlaceholders(string eventType)
    {
        var placeholders = new List<string>
        {
            // Event metadata (always available)
            "event.type",
            "event.timestamp",
            "event.url"
        };

        // Add event-specific placeholders based on event type
        if (eventType.StartsWith("task."))
        {
            placeholders.AddRange(new[]
            {
                "task.title",
                "task.description",
                "task.id",
                "task.done",
                "project.title",
                "project.id",
                "project.description",
                "assignees",
                "assignee.count",
                "labels",
                "label.count"
            });
        }
        else if (eventType.StartsWith("project."))
        {
            placeholders.AddRange(new[]
            {
                "project.title",
                "project.id",
                "project.description"
            });
        }
        else if (eventType.StartsWith("team."))
        {
            placeholders.AddRange(new[]
            {
                "user.name",
                "user.username",
                "user.email"
            });
        }

        return placeholders;
    }

    private string? GetPlaceholderValue(string placeholder, TemplateContext context)
    {
        var parts = placeholder.Split('.');
        
        if (parts.Length < 2)
        {
            return null;
        }

        var category = parts[0];
        var property = parts[1];

        return category switch
        {
            "task" => GetTaskProperty(property, context.Task),
            "project" => GetProjectProperty(property, context.Project),
            "user" => GetUserProperty(property, context.User),
            "event" => GetEventProperty(property, context.Event),
            "assignees" => context.Assignees != null ? string.Join(", ", context.Assignees) : null,
            "assignee" when property == "count" => context.Assignees?.Count.ToString(),
            "labels" => context.Labels != null ? string.Join(", ", context.Labels) : null,
            "label" when property == "count" => context.Labels?.Count.ToString(),
            _ => null
        };
    }

    private string? GetTaskProperty(string property, TaskData? task)
    {
        if (task == null)
        {
            return null;
        }

        return property switch
        {
            "title" => task.Title,
            "description" => task.Description,
            "id" => task.Id.ToString(),
            "done" => task.Done.ToString(),
            _ => null
        };
    }

    private string? GetProjectProperty(string property, ProjectData? project)
    {
        if (project == null)
        {
            return null;
        }

        return property switch
        {
            "title" => project.Title,
            "id" => project.Id.ToString(),
            "description" => project.Description,
            _ => null
        };
    }

    private string? GetUserProperty(string property, UserData? user)
    {
        if (user == null)
        {
            return null;
        }

        return property switch
        {
            "name" => user.Name,
            "username" => user.Username,
            "email" => user.Email,
            _ => null
        };
    }

    private string? GetEventProperty(string property, EventData? eventData)
    {
        if (eventData == null)
        {
            return null;
        }

        return property switch
        {
            "type" => eventData.Type,
            "timestamp" => eventData.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            "url" => eventData.Url,
            _ => null
        };
    }
}

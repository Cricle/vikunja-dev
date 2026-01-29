using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public partial class SimpleTemplateEngine
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
                "task.dueDate",
                "task.priority",
                "task.url",
                "project.title",
                "project.id",
                "project.description",
                "project.url",
                "assignees",
                "assignee.count",
                "labels",
                "label.count"
            });
            
            // Comment-specific placeholders
            if (eventType.Contains("comment"))
            {
                placeholders.AddRange(new[]
                {
                    "comment.id",
                    "comment.text",
                    "comment.author"
                });
            }
            
            // Attachment-specific placeholders
            if (eventType.Contains("attachment"))
            {
                placeholders.AddRange(new[]
                {
                    "attachment.id",
                    "attachment.fileName"
                });
            }
            
            // Relation-specific placeholders
            if (eventType.Contains("relation"))
            {
                placeholders.AddRange(new[]
                {
                    "relation.taskId",
                    "relation.relatedTaskId",
                    "relation.relationType"
                });
            }
        }
        else if (eventType.StartsWith("project."))
        {
            placeholders.AddRange(new[]
            {
                "project.title",
                "project.id",
                "project.description",
                "project.url"
            });
        }
        else if (eventType.StartsWith("team."))
        {
            placeholders.AddRange(new[]
            {
                "team.name",
                "team.id",
                "team.description",
                "user.name",
                "user.username",
                "user.email",
                "user.id"
            });
        }
        else if (eventType.StartsWith("label."))
        {
            placeholders.AddRange(new[]
            {
                "label.title",
                "label.id",
                "label.description"
            });
        }
        else if (eventType.StartsWith("user."))
        {
            placeholders.AddRange(new[]
            {
                "user.name",
                "user.username",
                "user.email",
                "user.id"
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
            "team" => GetTeamProperty(property, context.Team),
            "comment" => GetCommentProperty(property, context.Comment),
            "attachment" => GetAttachmentProperty(property, context.Attachment),
            "relation" => GetRelationProperty(property, context.Relation),
            "event" => GetEventProperty(property, context.Event),
            "assignees" => context.Assignees != null ? string.Join(", ", context.Assignees) : null,
            "assignee" when property == "count" => context.Assignees?.Count.ToString(),
            "labels" => context.Labels != null ? string.Join(", ", context.Labels) : null,
            "label" when property == "count" => context.Labels?.Count.ToString(),
            "label" => GetLabelProperty(property, context.Label),
            _ => null
        };
    }

    private string? GetTaskProperty(string property, TaskTemplateData? task)
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
            "done" => task.Done ? "✓ Done" : "○ Not Done",
            "dueDate" => task.DueDate,
            "priority" => task.Priority.ToString(),
            "url" => task.Url,
            _ => null
        };
    }

    private string? GetProjectProperty(string property, ProjectTemplateData? project)
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
            "url" => project.Url,
            _ => null
        };
    }

    private string? GetUserProperty(string property, UserTemplateData? user)
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
            "id" => user.Id.ToString(),
            _ => null
        };
    }

    private string? GetTeamProperty(string property, TeamTemplateData? team)
    {
        if (team == null)
        {
            return null;
        }

        return property switch
        {
            "name" => team.Name,
            "id" => team.Id.ToString(),
            "description" => team.Description,
            _ => null
        };
    }

    private string? GetLabelProperty(string property, LabelTemplateData? label)
    {
        if (label == null)
        {
            return null;
        }

        return property switch
        {
            "title" => label.Title,
            "id" => label.Id.ToString(),
            "description" => label.Description,
            _ => null
        };
    }

    private string? GetCommentProperty(string property, CommentTemplateData? comment)
    {
        if (comment == null)
        {
            return null;
        }

        return property switch
        {
            "id" => comment.Id.ToString(),
            "text" => comment.Text,
            "author" => comment.Author,
            _ => null
        };
    }

    private string? GetAttachmentProperty(string property, AttachmentTemplateData? attachment)
    {
        if (attachment == null)
        {
            return null;
        }

        return property switch
        {
            "id" => attachment.Id.ToString(),
            "fileName" => attachment.FileName,
            _ => null
        };
    }

    private string? GetRelationProperty(string property, RelationTemplateData? relation)
    {
        if (relation == null)
        {
            return null;
        }

        return property switch
        {
            "taskId" => relation.TaskId.ToString(),
            "relatedTaskId" => relation.RelatedTaskId.ToString(),
            "relationType" => relation.RelationType,
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

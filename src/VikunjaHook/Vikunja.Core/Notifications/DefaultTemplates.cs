using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public static class DefaultTemplates
{
    public static readonly Dictionary<string, NotificationTemplate> Templates = new()
    {
        [EventTypes.TaskCreated] = new()
        {
            EventType = EventTypes.TaskCreated,
            Title = "📝 New Task: {{task.title}}",
            Body = "A new task has been created in {{project.title}}\n\n" +
                   "Description: {{task.description}}\n" +
                   "Assignees: {{assignees}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskUpdated] = new()
        {
            EventType = EventTypes.TaskUpdated,
            Title = "✏️ Task Updated: {{task.title}}",
            Body = "Task in {{project.title}} has been updated\n\n" +
                   "Status: {{task.done}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskDeleted] = new()
        {
            EventType = EventTypes.TaskDeleted,
            Title = "🗑️ Task Deleted: {{task.title}}",
            Body = "A task has been deleted from {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskAssigned] = new()
        {
            EventType = EventTypes.TaskAssigned,
            Title = "👤 Task Assigned: {{task.title}}",
            Body = "You have been assigned to a task in {{project.title}}\n\n" +
                   "Description: {{task.description}}\n" +
                   "Assignees: {{assignees}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskCommentCreated] = new()
        {
            EventType = EventTypes.TaskCommentCreated,
            Title = "💬 New Comment on: {{task.title}}",
            Body = "A new comment has been added to a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskCommentUpdated] = new()
        {
            EventType = EventTypes.TaskCommentUpdated,
            Title = "💬 Comment Updated on: {{task.title}}",
            Body = "A comment has been updated on a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskCommentDeleted] = new()
        {
            EventType = EventTypes.TaskCommentDeleted,
            Title = "💬 Comment Deleted on: {{task.title}}",
            Body = "A comment has been deleted from a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskAttachmentCreated] = new()
        {
            EventType = EventTypes.TaskAttachmentCreated,
            Title = "📎 Attachment Added to: {{task.title}}",
            Body = "A new attachment has been added to a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskAttachmentDeleted] = new()
        {
            EventType = EventTypes.TaskAttachmentDeleted,
            Title = "📎 Attachment Removed from: {{task.title}}",
            Body = "An attachment has been removed from a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskRelationCreated] = new()
        {
            EventType = EventTypes.TaskRelationCreated,
            Title = "🔗 Task Relation Created: {{task.title}}",
            Body = "A new relation has been created for a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskRelationDeleted] = new()
        {
            EventType = EventTypes.TaskRelationDeleted,
            Title = "🔗 Task Relation Removed: {{task.title}}",
            Body = "A relation has been removed from a task in {{project.title}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.ProjectCreated] = new()
        {
            EventType = EventTypes.ProjectCreated,
            Title = "📁 New Project: {{project.title}}",
            Body = "A new project has been created\n\n" +
                   "Description: {{project.description}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.ProjectUpdated] = new()
        {
            EventType = EventTypes.ProjectUpdated,
            Title = "📁 Project Updated: {{project.title}}",
            Body = "Project {{project.title}} has been updated\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.ProjectDeleted] = new()
        {
            EventType = EventTypes.ProjectDeleted,
            Title = "📁 Project Deleted: {{project.title}}",
            Body = "Project {{project.title}} has been deleted\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TeamMemberAdded] = new()
        {
            EventType = EventTypes.TeamMemberAdded,
            Title = "👥 Team Member Added",
            Body = "{{user.name}} has been added to the team",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TeamMemberRemoved] = new()
        {
            EventType = EventTypes.TeamMemberRemoved,
            Title = "👥 Team Member Removed",
            Body = "{{user.name}} has been removed from the team",
            Format = NotificationFormat.Text
        }
    };
}

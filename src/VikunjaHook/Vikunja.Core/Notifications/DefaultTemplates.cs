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
            Body = "Event: {{event.type}}\n" +
                   "A new task has been created in {{project.title}}\n\n" +
                   "Task ID: {{task.id}}\n" +
                   "Description: {{task.description}}\n" +
                   "Priority: {{task.priority}}\n" +
                   "Due Date: {{task.dueDate}}\n" +
                   "Assignees ({{assignee.count}}): {{assignees}}\n" +
                   "Labels ({{label.count}}): {{labels}}\n" +
                   "Link: {{event.url}}\n" +
                   "Task URL: {{task.url}}\n" +
                   "Project URL: {{project.url}}\n" +
                   "Project Description: {{project.description}}\n" +
                   "Event Time: {{event.timestamp}}",
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
        
        ["task.assignee.created"] = new()
        {
            EventType = "task.assignee.created",
            Title = "👤 Assignee Added: {{task.title}}",
            Body = "An assignee has been added to a task in {{project.title}}\n\n" +
                   "Task: {{task.title}}\n" +
                   "Assignees: {{assignees}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TaskCommentCreated] = new()
        {
            EventType = EventTypes.TaskCommentCreated,
            Title = "💬 New Comment on: {{task.title}}",
            Body = "A new comment has been added to a task in {{project.title}}\n\n" +
                   "Comment: {{comment.text}}\n" +
                   "Author: {{comment.author}}\n" +
                   "Comment ID: {{comment.id}}\n" +
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
            Body = "A new attachment has been added to a task in {{project.title}}\n\n" +
                   "File: {{attachment.fileName}}\n" +
                   "Attachment ID: {{attachment.id}}\n" +
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
            Body = "A new relation has been created for a task in {{project.title}}\n\n" +
                   "Task ID: {{relation.taskId}}\n" +
                   "Related Task ID: {{relation.relatedTaskId}}\n" +
                   "Relation Type: {{relation.relationType}}\n" +
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
                   "Project Information:\n" +
                   "- Title: {{project.title}}\n" +
                   "- Project ID: {{project.id}}\n" +
                   "- Description: {{project.description}}\n" +
                   "- URL: {{project.url}}\n\n" +
                   "Event Time: {{event.timestamp}}\n" +
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
            Body = "A new member has been added to the team\n\n" +
                   "User Information:\n" +
                   "- Name: {{user.name}}\n" +
                   "- Username: {{user.username}}\n" +
                   "- Email: {{user.email}}\n" +
                   "- User ID: {{user.id}}\n\n" +
                   "Team Information:\n" +
                   "- Team Name: {{team.name}}\n" +
                   "- Team ID: {{team.id}}\n" +
                   "- Description: {{team.description}}\n\n" +
                   "Event Time: {{event.timestamp}}",
            Format = NotificationFormat.Text
        },
        
        [EventTypes.TeamMemberRemoved] = new()
        {
            EventType = EventTypes.TeamMemberRemoved,
            Title = "👥 Team Member Removed",
            Body = "A member has been removed from the team\n\n" +
                   "User Information:\n" +
                   "- Name: {{user.name}}\n" +
                   "- Username: {{user.username}}\n" +
                   "- Email: {{user.email}}\n" +
                   "- User ID: {{user.id}}\n\n" +
                   "Team Information:\n" +
                   "- Team Name: {{team.name}}\n" +
                   "- Team ID: {{team.id}}\n" +
                   "- Description: {{team.description}}\n\n" +
                   "Event Time: {{event.timestamp}}",
            Format = NotificationFormat.Text
        },
        
        // Label events
        ["label.created"] = new()
        {
            EventType = "label.created",
            Title = "🏷️ Label Created: {{label.title}}",
            Body = "A new label has been created\n\n" +
                   "Label Information:\n" +
                   "- Title: {{label.title}}\n" +
                   "- Label ID: {{label.id}}\n" +
                   "- Description: {{label.description}}\n\n" +
                   "Event: {{event.type}}\n" +
                   "Event Time: {{event.timestamp}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        ["label.updated"] = new()
        {
            EventType = "label.updated",
            Title = "🏷️ Label Updated: {{label.title}}",
            Body = "A label has been updated\n\n" +
                   "Label Information:\n" +
                   "- Title: {{label.title}}\n" +
                   "- Label ID: {{label.id}}\n" +
                   "- Description: {{label.description}}\n\n" +
                   "Event Time: {{event.timestamp}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        ["label.deleted"] = new()
        {
            EventType = "label.deleted",
            Title = "🏷️ Label Deleted: {{label.title}}",
            Body = "A label has been deleted\n\n" +
                   "Label Information:\n" +
                   "- Title: {{label.title}}\n" +
                   "- Label ID: {{label.id}}\n\n" +
                   "Event Time: {{event.timestamp}}",
            Format = NotificationFormat.Text
        },
        
        // Team events
        ["team.created"] = new()
        {
            EventType = "team.created",
            Title = "👥 Team Created: {{team.name}}",
            Body = "A new team has been created\n\n" +
                   "Team Information:\n" +
                   "- Name: {{team.name}}\n" +
                   "- Team ID: {{team.id}}\n" +
                   "- Description: {{team.description}}\n\n" +
                   "Event Time: {{event.timestamp}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        ["team.updated"] = new()
        {
            EventType = "team.updated",
            Title = "👥 Team Updated: {{team.name}}",
            Body = "A team has been updated\n\n" +
                   "Team Information:\n" +
                   "- Name: {{team.name}}\n" +
                   "- Team ID: {{team.id}}\n" +
                   "- Description: {{team.description}}\n\n" +
                   "Event Time: {{event.timestamp}}\n" +
                   "Link: {{event.url}}",
            Format = NotificationFormat.Text
        },
        
        ["team.deleted"] = new()
        {
            EventType = "team.deleted",
            Title = "👥 Team Deleted: {{team.name}}",
            Body = "A team has been deleted\n\n" +
                   "Team Information:\n" +
                   "- Name: {{team.name}}\n" +
                   "- Team ID: {{team.id}}\n\n" +
                   "Event Time: {{event.timestamp}}",
            Format = NotificationFormat.Text
        },
        
        // User events
        ["user.created"] = new()
        {
            EventType = "user.created",
            Title = "👤 User Created: {{user.username}}",
            Body = "A new user has been created\n\n" +
                   "User Information:\n" +
                   "- Name: {{user.name}}\n" +
                   "- Username: {{user.username}}\n" +
                   "- Email: {{user.email}}\n" +
                   "- User ID: {{user.id}}\n\n" +
                   "Event Time: {{event.timestamp}}",
            Format = NotificationFormat.Text
        }
    };
}

using System.Text.Json;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Models.Requests;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tool for managing Vikunja tasks
/// </summary>
public class TasksTool : IMcpTool
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly IAuthenticationManager _authManager;
    private readonly ILogger<TasksTool> _logger;

    public string Name => "tasks";
    public string Description => "Manage Vikunja tasks: create, get, update, delete, list";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "list",
        "create",
        "get",
        "update",
        "delete",
        "assign",
        "unassign",
        "list-assignees",
        "comment",
        "list-comments",
        "relate",
        "unrelate",
        "relations",
        "add-reminder",
        "remove-reminder",
        "list-reminders",
        "apply-label",
        "remove-label",
        "list-labels",
        "bulk-create",
        "bulk-update",
        "bulk-delete"
    };

    public TasksTool(
        IVikunjaClientFactory clientFactory,
        IAuthenticationManager authManager,
        ILogger<TasksTool> logger)
    {
        _clientFactory = clientFactory;
        _authManager = authManager;
        _logger = logger;
    }

    public async Task<object?> ExecuteAsync(
        string subcommand,
        Dictionary<string, object?> parameters,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = _authManager.GetSession(sessionId);
        if (session == null)
        {
            throw new AuthenticationException("Invalid session");
        }

        return subcommand switch
        {
            "list" => await ListTasksAsync(session, parameters, cancellationToken),
            "create" => await CreateTaskAsync(session, parameters, cancellationToken),
            "get" => await GetTaskAsync(session, parameters, cancellationToken),
            "update" => await UpdateTaskAsync(session, parameters, cancellationToken),
            "delete" => await DeleteTaskAsync(session, parameters, cancellationToken),
            "assign" => await AssignUsersAsync(session, parameters, cancellationToken),
            "unassign" => await UnassignUsersAsync(session, parameters, cancellationToken),
            "list-assignees" => await ListAssigneesAsync(session, parameters, cancellationToken),
            "comment" => await AddCommentAsync(session, parameters, cancellationToken),
            "list-comments" => await ListCommentsAsync(session, parameters, cancellationToken),
            "relate" => await RelateTasksAsync(session, parameters, cancellationToken),
            "unrelate" => await UnrelateTasksAsync(session, parameters, cancellationToken),
            "relations" => await ListRelationsAsync(session, parameters, cancellationToken),
            "add-reminder" => await AddReminderAsync(session, parameters, cancellationToken),
            "remove-reminder" => await RemoveReminderAsync(session, parameters, cancellationToken),
            "list-reminders" => await ListRemindersAsync(session, parameters, cancellationToken),
            "apply-label" => await ApplyLabelsAsync(session, parameters, cancellationToken),
            "remove-label" => await RemoveLabelsAsync(session, parameters, cancellationToken),
            "list-labels" => await ListTaskLabelsAsync(session, parameters, cancellationToken),
            "bulk-create" => await BulkCreateTasksAsync(session, parameters, cancellationToken),
            "bulk-update" => await BulkUpdateTasksAsync(session, parameters, cancellationToken),
            "bulk-delete" => await BulkDeleteTasksAsync(session, parameters, cancellationToken),
            _ => throw new ValidationException($"Unknown subcommand: {subcommand}")
        };
    }

    private async Task<object> ListTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing tasks for session {SessionId}", session.SessionId);

        // Extract query parameters
        var page = GetIntParameter(parameters, "page", 1);
        var perPage = GetIntParameter(parameters, "perPage", 50);
        var search = GetStringParameter(parameters, "search");
        var projectId = GetLongParameter(parameters, "projectId");

        // Build query string
        var queryParams = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"s={Uri.EscapeDataString(search)}");
        }

        var endpoint = projectId.HasValue
            ? $"projects/{projectId.Value}/tasks?{string.Join("&", queryParams)}"
            : $"tasks/all?{string.Join("&", queryParams)}";

        var tasks = await _clientFactory.GetAsync<List<VikunjaTask>>(
            session,
            endpoint,
            cancellationToken
        );

        return new
        {
            tasks = tasks ?? new List<VikunjaTask>(),
            count = tasks?.Count ?? 0,
            page,
            perPage
        };
    }

    private async Task<object> CreateTaskAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating task for session {SessionId}", session.SessionId);

        // Extract and validate required parameters
        var projectId = GetLongParameter(parameters, "projectId")
            ?? throw new ValidationException("projectId is required");
        var title = GetStringParameter(parameters, "title")
            ?? throw new ValidationException("title is required");

        // Build request
        var request = new CreateTaskRequest(
            ProjectId: projectId,
            Title: title,
            Description: GetStringParameter(parameters, "description"),
            DueDate: GetDateTimeParameter(parameters, "dueDate"),
            Priority: GetIntParameter(parameters, "priority"),
            Labels: GetLongListParameter(parameters, "labels"),
            Assignees: GetLongListParameter(parameters, "assignees"),
            RepeatAfter: GetIntParameter(parameters, "repeatAfter"),
            RepeatMode: GetStringParameter(parameters, "repeatMode")
        );

        var task = await _clientFactory.PutAsync<VikunjaTask>(
            session,
            $"projects/{projectId}/tasks",
            request,
            cancellationToken
        );

        return new
        {
            task,
            message = $"Task '{title}' created successfully"
        };
    }

    private async Task<object> GetTaskAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Getting task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (task == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        return new { task };
    }

    private async Task<object> UpdateTaskAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Updating task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        // Build update request with only provided fields
        var request = new UpdateTaskRequest(
            Id: taskId,
            Title: GetStringParameter(parameters, "title"),
            Description: GetStringParameter(parameters, "description"),
            Done: GetBoolParameter(parameters, "done"),
            Priority: GetIntParameter(parameters, "priority"),
            DueDate: GetDateTimeParameter(parameters, "dueDate"),
            Labels: GetLongListParameter(parameters, "labels"),
            Assignees: GetLongListParameter(parameters, "assignees")
        );

        var task = await _clientFactory.PostAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            request,
            cancellationToken
        );

        return new
        {
            task,
            message = "Task updated successfully"
        };
    }

    private async Task<object> DeleteTaskAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Deleting task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        await _clientFactory.DeleteAsync(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            message = $"Task {taskId} deleted successfully"
        };
    }

    private async Task<object> AssignUsersAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var assigneeIds = GetLongListParameter(parameters, "assignees")
            ?? throw new ValidationException("assignees list is required");

        if (assigneeIds.Count == 0)
        {
            throw new ValidationException("assignees list cannot be empty");
        }

        _logger.LogInformation("Assigning {Count} users to task {TaskId} for session {SessionId}",
            assigneeIds.Count, taskId, session.SessionId);

        // Vikunja API: POST /tasks/{id}/assignees/bulk with user_ids array
        var bulkAssignRequest = new BulkAssignRequest(assigneeIds);

        await _clientFactory.PutAsync<object>(
            session,
            $"tasks/{taskId}/assignees/bulk",
            bulkAssignRequest,
            cancellationToken
        );

        // Fetch updated task to get current assignees
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Assigned {assigneeIds.Count} user(s) to task {taskId}",
            assignedCount = assigneeIds.Count
        };
    }

    private async Task<object> UnassignUsersAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var userIds = GetLongListParameter(parameters, "assignees")
            ?? throw new ValidationException("assignees list is required");

        if (userIds.Count == 0)
        {
            throw new ValidationException("assignees list cannot be empty");
        }

        _logger.LogInformation("Unassigning {Count} users from task {TaskId} for session {SessionId}",
            userIds.Count, taskId, session.SessionId);

        // Remove each user individually (Vikunja API doesn't have bulk unassign)
        var removedCount = 0;
        foreach (var userId in userIds)
        {
            try
            {
                await _clientFactory.DeleteAsync(
                    session,
                    $"tasks/{taskId}/assignees/{userId}",
                    cancellationToken
                );
                removedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove user {UserId} from task {TaskId}", userId, taskId);
                // Continue with other users
            }
        }

        // Fetch updated task to get current assignees
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Removed {removedCount} user(s) from task {taskId}",
            removedCount,
            requestedCount = userIds.Count
        };
    }

    private async Task<object> ListAssigneesAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Listing assignees for task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (task == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        var assignees = task.Assignees ?? new List<VikunjaUser>();

        return new
        {
            taskId = task.Id,
            taskTitle = task.Title,
            assignees,
            count = assignees.Count
        };
    }

    private async Task<object> AddCommentAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var commentText = GetStringParameter(parameters, "comment")
            ?? throw new ValidationException("comment text is required");

        if (string.IsNullOrWhiteSpace(commentText))
        {
            throw new ValidationException("comment text cannot be empty");
        }

        _logger.LogInformation("Adding comment to task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var commentRequest = new AddCommentRequest(commentText);

        var comment = await _clientFactory.PutAsync<VikunjaComment>(
            session,
            $"tasks/{taskId}/comments",
            commentRequest,
            cancellationToken
        );

        return new
        {
            comment,
            message = "Comment added successfully"
        };
    }

    private async Task<object> ListCommentsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Listing comments for task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var comments = await _clientFactory.GetAsync<List<VikunjaComment>>(
            session,
            $"tasks/{taskId}/comments",
            cancellationToken
        );

        return new
        {
            taskId,
            comments = comments ?? new List<VikunjaComment>(),
            count = comments?.Count ?? 0
        };
    }

    private async Task<object> RelateTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var otherTaskId = GetLongParameter(parameters, "otherTaskId")
            ?? throw new ValidationException("otherTaskId is required");
        var relationKindStr = GetStringParameter(parameters, "relationKind")
            ?? throw new ValidationException("relationKind is required");

        // Validate and convert relation kind
        if (!Enum.TryParse<TaskRelationKind>(relationKindStr, true, out var relationKind))
        {
            throw new ValidationException($"Invalid relation kind: {relationKindStr}. Valid values: subtask, parenttask, related, duplicateof, duplicates, blocking, blocked, precedes, follows, copiedfrom, copiedto");
        }

        _logger.LogInformation("Creating {RelationKind} relation between task {TaskId} and {OtherTaskId} for session {SessionId}",
            relationKind, taskId, otherTaskId, session.SessionId);

        var relationRequest = new TaskRelationRequest(
            taskId,
            otherTaskId,
            relationKindStr.ToLowerInvariant()
        );

        await _clientFactory.PutAsync<object>(
            session,
            $"tasks/{taskId}/relations",
            relationRequest,
            cancellationToken
        );

        // Fetch updated task to show all relations
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Successfully created {relationKind} relation between task {taskId} and task {otherTaskId}"
        };
    }

    private async Task<object> UnrelateTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var otherTaskId = GetLongParameter(parameters, "otherTaskId")
            ?? throw new ValidationException("otherTaskId is required");
        var relationKindStr = GetStringParameter(parameters, "relationKind")
            ?? throw new ValidationException("relationKind is required");

        // Validate relation kind
        if (!Enum.TryParse<TaskRelationKind>(relationKindStr, true, out var relationKind))
        {
            throw new ValidationException($"Invalid relation kind: {relationKindStr}");
        }

        _logger.LogInformation("Removing {RelationKind} relation between task {TaskId} and {OtherTaskId} for session {SessionId}",
            relationKind, taskId, otherTaskId, session.SessionId);

        await _clientFactory.DeleteAsync(
            session,
            $"tasks/{taskId}/relations/{relationKindStr.ToLowerInvariant()}/{otherTaskId}",
            cancellationToken
        );

        // Fetch updated task to show remaining relations
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Successfully removed {relationKind} relation between task {taskId} and task {otherTaskId}"
        };
    }

    private async Task<object> ListRelationsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Listing relations for task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        // Get task with its relations (Vikunja includes related_tasks in task details)
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (task == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        var relatedTasks = task.RelatedTasks ?? new List<VikunjaTaskRelation>();

        return new
        {
            taskId = task.Id,
            taskTitle = task.Title,
            relatedTasks,
            count = relatedTasks.Count
        };
    }

    private async Task<object> AddReminderAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var reminderDateStr = GetStringParameter(parameters, "reminderDate")
            ?? throw new ValidationException("reminderDate is required");

        // Parse and validate date
        if (!DateTime.TryParse(reminderDateStr, out var reminderDate))
        {
            throw new ValidationException($"Invalid date format: {reminderDateStr}. Use ISO 8601 format (e.g., 2024-12-31T10:00:00Z)");
        }

        _logger.LogInformation("Adding reminder to task {TaskId} for {ReminderDate} for session {SessionId}",
            taskId, reminderDate, session.SessionId);

        // Get current task to preserve existing reminders
        var currentTask = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (currentTask == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        // Build updated reminders list
        var existingReminders = currentTask.Reminders ?? new List<VikunjaReminder>();
        var updatedReminders = new List<ReminderItem>();

        foreach (var reminder in existingReminders)
        {
            updatedReminders.Add(new ReminderItem(reminder.Reminder));
        }

        // Add new reminder
        updatedReminders.Add(new ReminderItem(reminderDate));

        // Update task with new reminders
        var updateRequest = new UpdateTaskRemindersRequest(updatedReminders);

        var updatedTask = await _clientFactory.PostAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            updateRequest,
            cancellationToken
        );

        return new
        {
            task = updatedTask,
            message = $"Reminder added successfully for {reminderDate:yyyy-MM-dd HH:mm:ss}"
        };
    }

    private async Task<object> RemoveReminderAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var reminderDateStr = GetStringParameter(parameters, "reminderDate")
            ?? throw new ValidationException("reminderDate is required");

        // Parse and validate date
        if (!DateTime.TryParse(reminderDateStr, out var reminderDate))
        {
            throw new ValidationException($"Invalid date format: {reminderDateStr}");
        }

        _logger.LogInformation("Removing reminder {ReminderDate} from task {TaskId} for session {SessionId}",
            reminderDate, taskId, session.SessionId);

        // Get current task
        var currentTask = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (currentTask == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        var existingReminders = currentTask.Reminders ?? new List<VikunjaReminder>();
        if (existingReminders.Count == 0)
        {
            throw new ValidationException("Task has no reminders to remove");
        }

        // Find the closest matching reminder (within 12 hours tolerance to account for timezone differences)
        var closestReminder = existingReminders
            .OrderBy(r => Math.Abs((r.Reminder - reminderDate).TotalSeconds))
            .FirstOrDefault();

        if (closestReminder == null || Math.Abs((closestReminder.Reminder - reminderDate).TotalHours) > 12)
        {
            throw new ValidationException($"No reminder found close to {reminderDate:yyyy-MM-dd HH:mm:ss}. Available reminders: {string.Join(", ", existingReminders.Select(r => r.Reminder.ToString("yyyy-MM-dd HH:mm:ss")))}");
        }

        // Filter out the closest matching reminder
        var updatedReminders = existingReminders
            .Where(r => r.Reminder != closestReminder.Reminder)
            .Select(r => new ReminderItem(r.Reminder))
            .ToList();

        // Update task with filtered reminders
        var updateRequest = new UpdateTaskRemindersRequest(updatedReminders);

        var updatedTask = await _clientFactory.PostAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            updateRequest,
            cancellationToken
        );

        return new
        {
            task = updatedTask,
            message = $"Reminder removed successfully"
        };
    }

    private async Task<object> ListRemindersAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Listing reminders for task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (task == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        var reminders = task.Reminders ?? new List<VikunjaReminder>();

        return new
        {
            taskId = task.Id,
            taskTitle = task.Title,
            reminders,
            count = reminders.Count
        };
    }

    private async Task<object> ApplyLabelsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var labelIds = GetLongListParameter(parameters, "labels")
            ?? throw new ValidationException("labels list is required");

        if (labelIds.Count == 0)
        {
            throw new ValidationException("At least one label id is required");
        }

        _logger.LogInformation("Applying {Count} labels to task {TaskId} for session {SessionId}",
            labelIds.Count, taskId, session.SessionId);

        // Add each label individually
        var appliedCount = 0;
        foreach (var labelId in labelIds)
        {
            try
            {
                var labelRequest = new ApplyLabelRequest(
                    TaskId: taskId,
                    LabelId: labelId
                );

                await _clientFactory.PutAsync<object>(
                    session,
                    $"tasks/{taskId}/labels",
                    labelRequest,
                    cancellationToken
                );
                appliedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply label {LabelId} to task {TaskId}", labelId, taskId);
                // Continue with other labels
            }
        }

        // Fetch updated task to show current labels
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Applied {appliedCount} label(s) to task {taskId}",
            appliedCount,
            requestedCount = labelIds.Count
        };
    }

    private async Task<object> RemoveLabelsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var labelIds = GetLongListParameter(parameters, "labels")
            ?? throw new ValidationException("labels list is required");

        if (labelIds.Count == 0)
        {
            throw new ValidationException("At least one label id is required to remove");
        }

        _logger.LogInformation("Removing {Count} labels from task {TaskId} for session {SessionId}",
            labelIds.Count, taskId, session.SessionId);

        // Remove each label individually
        var removedCount = 0;
        foreach (var labelId in labelIds)
        {
            try
            {
                await _clientFactory.DeleteAsync(
                    session,
                    $"tasks/{taskId}/labels/{labelId}",
                    cancellationToken
                );
                removedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove label {LabelId} from task {TaskId}", labelId, taskId);
                // Continue with other labels
            }
        }

        // Fetch updated task to show current labels
        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        return new
        {
            task,
            message = $"Removed {removedCount} label(s) from task {taskId}",
            removedCount,
            requestedCount = labelIds.Count
        };
    }

    private async Task<object> ListTaskLabelsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Listing labels for task {TaskId} for session {SessionId}",
            taskId, session.SessionId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            session,
            $"tasks/{taskId}",
            cancellationToken
        );

        if (task == null)
        {
            throw new ResourceNotFoundException("Task", taskId);
        }

        var labels = task.Labels ?? new List<VikunjaLabel>();

        return new
        {
            taskId = task.Id,
            taskTitle = task.Title,
            labels,
            count = labels.Count
        };
    }

    private const int MaxBulkOperationTasks = 100;

    private async Task<object> BulkCreateTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "projectId")
            ?? throw new ValidationException("projectId is required for bulk create");

        // Extract tasks array from parameters
        if (!parameters.TryGetValue("tasks", out var tasksObj) || tasksObj == null)
        {
            throw new ValidationException("tasks array is required");
        }

        JsonElement tasksJson;
        if (tasksObj is JsonElement element)
        {
            tasksJson = element;
        }
        else
        {
            // For AOT compatibility, avoid SerializeToElement
            throw new ValidationException("tasks parameter must be a JSON array");
        }

        if (tasksJson.ValueKind != JsonValueKind.Array)
        {
            throw new ValidationException("tasks must be an array");
        }

        var tasksList = new List<CreateTaskRequest>();
        foreach (var taskElement in tasksJson.EnumerateArray())
        {
            var title = taskElement.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                ? titleProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException("Each task must have a non-empty title");
            }

            tasksList.Add(new CreateTaskRequest(
                ProjectId: projectId,
                Title: title,
                Description: taskElement.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String ? desc.GetString() : null,
                DueDate: taskElement.TryGetProperty("dueDate", out var due) && due.ValueKind == JsonValueKind.String && DateTime.TryParse(due.GetString(), out var dueDate) ? dueDate : null,
                Priority: taskElement.TryGetProperty("priority", out var pri) && pri.ValueKind == JsonValueKind.Number && pri.TryGetInt32(out var priority) ? priority : null,
                Labels: null,
                Assignees: null,
                RepeatAfter: null,
                RepeatMode: null
            ));
        }

        if (tasksList.Count == 0)
        {
            throw new ValidationException("tasks array must contain at least one task");
        }

        if (tasksList.Count > MaxBulkOperationTasks)
        {
            throw new ValidationException($"Too many tasks for bulk operation. Maximum allowed: {MaxBulkOperationTasks}");
        }

        _logger.LogInformation("Bulk creating {Count} tasks in project {ProjectId} for session {SessionId}",
            tasksList.Count, projectId, session.SessionId);

        var createdTasks = new List<VikunjaTask>();
        var failedCount = 0;

        foreach (var taskRequest in tasksList)
        {
            try
            {
                var task = await _clientFactory.PutAsync<VikunjaTask>(
                    session,
                    $"projects/{projectId}/tasks",
                    taskRequest,
                    cancellationToken
                );

                if (task != null)
                {
                    createdTasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create task: {Title}", taskRequest.Title);
                failedCount++;
            }
        }

        return new
        {
            tasks = createdTasks,
            message = $"Successfully created {createdTasks.Count} tasks{(failedCount > 0 ? $", {failedCount} failed" : "")}",
            successCount = createdTasks.Count,
            failedCount,
            totalRequested = tasksList.Count
        };
    }

    // Helper methods for parameter extraction
    private static string? GetStringParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            return value is JsonElement element && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : value.ToString();
        }
        return null;
    }

    private static long? GetLongParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var num))
                    return num;
            }
            else if (value is long l)
                return l;
            else if (value is int i)
                return i;
            else if (long.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static int? GetIntParameter(Dictionary<string, object?> parameters, string key, int? defaultValue = null)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var num))
                    return num;
            }
            else if (value is int i)
                return i;
            else if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    private static bool? GetBoolParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return element.GetBoolean();
            }
            else if (value is bool b)
                return b;
            else if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static DateTime? GetDateTimeParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            var str = value is JsonElement element && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : value.ToString();

            if (!string.IsNullOrWhiteSpace(str) && DateTime.TryParse(str, out var date))
                return date;
        }
        return null;
    }

    private static List<long>? GetLongListParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<long>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var num))
                        list.Add(num);
                }
                return list.Count > 0 ? list : null;
            }
        }
        return null;
    }

    private async Task<object> BulkUpdateTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskIds = GetLongListParameter(parameters, "taskIds")
            ?? throw new ValidationException("taskIds array is required");
        var field = GetStringParameter(parameters, "field")
            ?? throw new ValidationException("field is required");

        if (!parameters.TryGetValue("value", out var value))
        {
            throw new ValidationException("value is required");
        }

        if (taskIds.Count == 0)
        {
            throw new ValidationException("taskIds array must contain at least one task ID");
        }

        if (taskIds.Count > MaxBulkOperationTasks)
        {
            throw new ValidationException($"Too many tasks for bulk operation. Maximum allowed: {MaxBulkOperationTasks}");
        }

        // Validate field
        var allowedFields = new[] { "done", "priority", "dueDate", "projectId" };
        if (!allowedFields.Contains(field))
        {
            throw new ValidationException($"Invalid field: {field}. Allowed fields: {string.Join(", ", allowedFields)}");
        }

        _logger.LogInformation("Bulk updating {Count} tasks, field: {Field} for session {SessionId}",
            taskIds.Count, field, session.SessionId);

        var updatedTasks = new List<VikunjaTask>();
        var failedCount = 0;

        foreach (var taskId in taskIds)
        {
            try
            {
                // Build update request based on field
                object updateRequest = field switch
                {
                    "done" => new BulkUpdateTaskDoneRequest(
                        value is JsonElement el && el.ValueKind == JsonValueKind.True || (value is bool b && b)
                    ),
                    "priority" => new BulkUpdateTaskPriorityRequest(
                        value is JsonElement el && el.TryGetInt32(out var p) ? p : value is int i ? i : 0
                    ),
                    "dueDate" => new BulkUpdateTaskDueDateRequest(
                        value?.ToString()
                    ),
                    "projectId" => new BulkUpdateTaskProjectRequest(
                        value is JsonElement el && el.TryGetInt64(out var pid) ? pid : value is long l ? l : 0
                    ),
                    _ => throw new ValidationException($"Invalid field: {field}")
                };

                var task = await _clientFactory.PostAsync<VikunjaTask>(
                    session,
                    $"tasks/{taskId}",
                    updateRequest,
                    cancellationToken
                );

                if (task != null)
                {
                    updatedTasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update task {TaskId}", taskId);
                failedCount++;
            }
        }

        return new
        {
            tasks = updatedTasks,
            message = $"Successfully updated {updatedTasks.Count} tasks{(failedCount > 0 ? $", {failedCount} failed" : "")}",
            successCount = updatedTasks.Count,
            failedCount,
            totalRequested = taskIds.Count,
            field,
            value
        };
    }

    private async Task<object> BulkDeleteTasksAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var taskIds = GetLongListParameter(parameters, "taskIds")
            ?? throw new ValidationException("taskIds array is required");

        if (taskIds.Count == 0)
        {
            throw new ValidationException("taskIds array must contain at least one task ID");
        }

        if (taskIds.Count > MaxBulkOperationTasks)
        {
            throw new ValidationException($"Too many tasks for bulk operation. Maximum allowed: {MaxBulkOperationTasks}");
        }

        _logger.LogInformation("Bulk deleting {Count} tasks for session {SessionId}",
            taskIds.Count, session.SessionId);

        var deletedCount = 0;
        var failedCount = 0;
        var failedIds = new List<long>();

        foreach (var taskId in taskIds)
        {
            try
            {
                await _clientFactory.DeleteAsync(
                    session,
                    $"tasks/{taskId}",
                    cancellationToken
                );
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete task {TaskId}", taskId);
                failedCount++;
                failedIds.Add(taskId);
            }
        }

        return new
        {
            message = $"Successfully deleted {deletedCount} tasks{(failedCount > 0 ? $", {failedCount} failed" : "")}",
            deletedCount,
            failedCount,
            totalRequested = taskIds.Count,
            deletedTaskIds = taskIds.Except(failedIds).ToList(),
            failedTaskIds = failedIds
        };
    }
}

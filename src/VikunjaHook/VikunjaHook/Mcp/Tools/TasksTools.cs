using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tools for managing Vikunja tasks
/// </summary>
internal class TasksTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TasksTools> _logger;

    public TasksTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TasksTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all tasks in a project or across all projects")]
    public async Task<List<VikunjaTask>> ListTasks(
        [Description("Project ID to filter tasks (optional)")] long? projectId = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        [Description("Search query (optional)")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing tasks - projectId: {ProjectId}, page: {Page}", projectId, page);

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

        var tasks = await _clientFactory.GetAsync<List<VikunjaTask>>(endpoint, cancellationToken);
        return tasks ?? new List<VikunjaTask>();
    }

    [McpServerTool]
    [Description("Create a new task in a project")]
    public async Task<VikunjaTask> CreateTask(
        [Description("Project ID where the task will be created")] long projectId,
        [Description("Task title")] string title,
        [Description("Task description (optional)")] string? description = null,
        [Description("Due date in ISO 8601 format (optional)")] string? dueDate = null,
        [Description("Priority (0-5, default: 0)")] int? priority = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating task '{Title}' in project {ProjectId}", title, projectId);

        DateTime? parsedDueDate = null;
        if (!string.IsNullOrWhiteSpace(dueDate) && DateTime.TryParse(dueDate, out var date))
        {
            parsedDueDate = date;
        }

        var request = new
        {
            project_id = projectId,
            title,
            description,
            due_date = parsedDueDate,
            priority
        };

        var task = await _clientFactory.PutAsync<VikunjaTask>(
            $"projects/{projectId}/tasks",
            request,
            cancellationToken
        );

        return task;
    }

    [McpServerTool]
    [Description("Get details of a specific task")]
    public async Task<VikunjaTask> GetTask(
        [Description("Task ID")] long taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting task {TaskId}", taskId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            $"tasks/{taskId}",
            cancellationToken
        );

        return task;
    }

    [McpServerTool]
    [Description("Update an existing task")]
    public async Task<VikunjaTask> UpdateTask(
        [Description("Task ID")] long taskId,
        [Description("New title (optional)")] string? title = null,
        [Description("New description (optional)")] string? description = null,
        [Description("Mark as done (optional)")] bool? done = null,
        [Description("New priority (0-5, optional)")] int? priority = null,
        [Description("New due date in ISO 8601 format (optional)")] string? dueDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating task {TaskId}", taskId);

        DateTime? parsedDueDate = null;
        if (!string.IsNullOrWhiteSpace(dueDate) && DateTime.TryParse(dueDate, out var date))
        {
            parsedDueDate = date;
        }

        var request = new
        {
            id = taskId,
            title,
            description,
            done,
            priority,
            due_date = parsedDueDate
        };

        var task = await _clientFactory.PostAsync<VikunjaTask>(
            $"tasks/{taskId}",
            request,
            cancellationToken
        );

        return task;
    }

    [McpServerTool]
    [Description("Delete a task")]
    public async Task<string> DeleteTask(
        [Description("Task ID")] long taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting task {TaskId}", taskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}",
            cancellationToken
        );

        return $"Task {taskId} deleted successfully";
    }
}

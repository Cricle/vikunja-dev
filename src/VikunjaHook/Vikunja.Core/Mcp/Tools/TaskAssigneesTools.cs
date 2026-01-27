using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing task assignees
/// </summary>
public class TaskAssigneesTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TaskAssigneesTools> _logger;

    public TaskAssigneesTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TaskAssigneesTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Add an assignee to a task")]
    public async Task<VikunjaUser> AddTaskAssignee(
        [Description("Task ID")] long taskId,
        [Description("User ID to assign")] long userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding assignee {UserId} to task {TaskId}", userId, taskId);

        var request = new { user_id = userId };
        var user = await _clientFactory.PutAsync<VikunjaUser>(
            $"tasks/{taskId}/assignees",
            request,
            cancellationToken
        );

        return user;
    }

    [McpServerTool]
    [Description("Remove an assignee from a task")]
    public async Task<string> RemoveTaskAssignee(
        [Description("Task ID")] long taskId,
        [Description("User ID to remove")] long userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing assignee {UserId} from task {TaskId}", userId, taskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}/assignees/{userId}",
            cancellationToken
        );

        return $"Assignee {userId} removed from task {taskId}";
    }

    [McpServerTool]
    [Description("List all assignees of a task")]
    public async Task<List<VikunjaUser>> ListTaskAssignees(
        [Description("Task ID")] long taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing assignees for task {TaskId}", taskId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            $"tasks/{taskId}",
            cancellationToken
        );

        return task.Assignees ?? new List<VikunjaUser>();
    }
}

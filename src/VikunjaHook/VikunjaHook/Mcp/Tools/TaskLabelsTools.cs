using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tools for managing task labels
/// </summary>
internal class TaskLabelsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TaskLabelsTools> _logger;

    public TaskLabelsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TaskLabelsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Add a label to a task")]
    public async Task<VikunjaLabel> AddTaskLabel(
        [Description("Task ID")] long taskId,
        [Description("Label ID")] long labelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding label {LabelId} to task {TaskId}", labelId, taskId);

        var request = new { label_id = labelId };
        var label = await _clientFactory.PutAsync<VikunjaLabel>(
            $"tasks/{taskId}/labels",
            request,
            cancellationToken
        );

        return label;
    }

    [McpServerTool]
    [Description("Remove a label from a task")]
    public async Task<string> RemoveTaskLabel(
        [Description("Task ID")] long taskId,
        [Description("Label ID")] long labelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing label {LabelId} from task {TaskId}", labelId, taskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}/labels/{labelId}",
            cancellationToken
        );

        return $"Label {labelId} removed from task {taskId}";
    }

    [McpServerTool]
    [Description("List all labels on a task")]
    public async Task<List<VikunjaLabel>> ListTaskLabels(
        [Description("Task ID")] long taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing labels for task {TaskId}", taskId);

        var task = await _clientFactory.GetAsync<VikunjaTask>(
            $"tasks/{taskId}",
            cancellationToken
        );

        return task.Labels ?? new List<VikunjaLabel>();
    }
}

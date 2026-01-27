using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing task relations
/// </summary>
public class TaskRelationsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TaskRelationsTools> _logger;

    public TaskRelationsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TaskRelationsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("Create a relation between two tasks")]
    public async Task<VikunjaTaskRelation> CreateTaskRelation(
        [Description("Task ID")] long taskId,
        [Description("Related task ID")] long otherTaskId,
        [Description("Relation kind: subtask, parenttask, related, duplicateof, duplicates, blocking, blocked, precedes, follows, copiedfrom, copiedto")] string relationKind,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating {RelationKind} relation between task {TaskId} and {OtherTaskId}", 
            relationKind, taskId, otherTaskId);

        var request = new
        {
            other_task_id = otherTaskId,
            relation_kind = relationKind
        };

        var relation = await _clientFactory.PutAsync<VikunjaTaskRelation>(
            $"tasks/{taskId}/relations",
            request,
            cancellationToken
        );

        return relation;
    }

    [McpServerTool]
    [Description("Delete a relation between two tasks")]
    public async Task<string> DeleteTaskRelation(
        [Description("Task ID")] long taskId,
        [Description("Related task ID")] long otherTaskId,
        [Description("Relation kind")] string relationKind,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting {RelationKind} relation between task {TaskId} and {OtherTaskId}", 
            relationKind, taskId, otherTaskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}/relations/{relationKind}/{otherTaskId}",
            cancellationToken
        );

        return $"Relation {relationKind} deleted between task {taskId} and {otherTaskId}";
    }
}

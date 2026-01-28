using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Models.Requests;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing task comments
/// </summary>
public class TaskCommentsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TaskCommentsTools> _logger;

    public TaskCommentsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TaskCommentsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all comments on a task")]
    public async Task<List<VikunjaComment>> ListTaskComments(
        [Description("Task ID")] long taskId,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing comments for task {TaskId}", taskId);

        var queryParams = $"page={page}&per_page={perPage}";
        var comments = await _clientFactory.GetAsync<List<VikunjaComment>>(
            $"tasks/{taskId}/comments?{queryParams}",
            cancellationToken
        );

        return comments ?? new List<VikunjaComment>();
    }

    [McpServerTool]
    [Description("Create a comment on a task")]
    public async Task<VikunjaComment> CreateTaskComment(
        [Description("Task ID")] long taskId,
        [Description("Comment text")] string comment,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating comment on task {TaskId}", taskId);

        var request = new CreateTaskCommentRequest { Comment = comment };
        var createdComment = await _clientFactory.PutAsync<VikunjaComment>(
            $"tasks/{taskId}/comments",
            request,
            cancellationToken
        );

        return createdComment;
    }

    [McpServerTool]
    [Description("Get a specific comment")]
    public async Task<VikunjaComment> GetTaskComment(
        [Description("Task ID")] long taskId,
        [Description("Comment ID")] long commentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting comment {CommentId} from task {TaskId}", commentId, taskId);

        var comment = await _clientFactory.GetAsync<VikunjaComment>(
            $"tasks/{taskId}/comments/{commentId}",
            cancellationToken
        );

        return comment;
    }

    [McpServerTool]
    [Description("Update a comment on a task")]
    public async Task<VikunjaComment> UpdateTaskComment(
        [Description("Task ID")] long taskId,
        [Description("Comment ID")] long commentId,
        [Description("New comment text")] string comment,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating comment {CommentId} on task {TaskId}", commentId, taskId);

        var request = new UpdateTaskCommentRequest { Comment = comment };
        var updatedComment = await _clientFactory.PostAsync<VikunjaComment>(
            $"tasks/{taskId}/comments/{commentId}",
            request,
            cancellationToken
        );

        return updatedComment;
    }

    [McpServerTool]
    [Description("Delete a comment from a task")]
    public async Task<string> DeleteTaskComment(
        [Description("Task ID")] long taskId,
        [Description("Comment ID")] long commentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting comment {CommentId} from task {TaskId}", commentId, taskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}/comments/{commentId}",
            cancellationToken
        );

        return $"Comment {commentId} deleted from task {taskId}";
    }
}

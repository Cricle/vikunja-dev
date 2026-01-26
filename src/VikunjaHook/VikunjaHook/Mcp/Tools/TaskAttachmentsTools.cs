using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// Represents a task attachment
/// </summary>
public record VikunjaAttachment(
    long Id,
    long TaskId,
    string FileName,
    long FileSize,
    string? MimeType,
    DateTime Created,
    VikunjaUser? CreatedBy
);

/// <summary>
/// MCP tools for managing task attachments
/// </summary>
internal class TaskAttachmentsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<TaskAttachmentsTools> _logger;

    public TaskAttachmentsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<TaskAttachmentsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all attachments on a task")]
    public async Task<List<VikunjaAttachment>> ListTaskAttachments(
        [Description("Task ID")] long taskId,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing attachments for task {TaskId}", taskId);

        var queryParams = $"page={page}&per_page={perPage}";
        var attachments = await _clientFactory.GetAsync<List<VikunjaAttachment>>(
            $"tasks/{taskId}/attachments?{queryParams}",
            cancellationToken
        );

        return attachments ?? new List<VikunjaAttachment>();
    }

    [McpServerTool]
    [Description("Get attachment information")]
    public async Task<VikunjaAttachment> GetTaskAttachment(
        [Description("Task ID")] long taskId,
        [Description("Attachment ID")] long attachmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting attachment {AttachmentId} from task {TaskId}", attachmentId, taskId);

        var attachment = await _clientFactory.GetAsync<VikunjaAttachment>(
            $"tasks/{taskId}/attachments/{attachmentId}",
            cancellationToken
        );

        return attachment;
    }

    [McpServerTool]
    [Description("Delete an attachment from a task")]
    public async Task<string> DeleteTaskAttachment(
        [Description("Task ID")] long taskId,
        [Description("Attachment ID")] long attachmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting attachment {AttachmentId} from task {TaskId}", attachmentId, taskId);

        await _clientFactory.DeleteAsync(
            $"tasks/{taskId}/attachments/{attachmentId}",
            cancellationToken
        );

        return $"Attachment {attachmentId} deleted from task {taskId}";
    }
}

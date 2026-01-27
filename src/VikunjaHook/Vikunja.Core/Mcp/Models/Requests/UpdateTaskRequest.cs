namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to update an existing task
/// </summary>
public record UpdateTaskRequest(
    long Id,
    string? Title = null,
    string? Description = null,
    bool? Done = null,
    int? Priority = null,
    DateTime? DueDate = null,
    List<long>? Labels = null,
    List<long>? Assignees = null
);

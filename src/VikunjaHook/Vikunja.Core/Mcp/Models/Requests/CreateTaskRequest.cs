namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to create a new task
/// </summary>
public record CreateTaskRequest(
    long ProjectId,
    string Title,
    string? Description = null,
    DateTime? DueDate = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? Priority = null,
    List<long>? Labels = null,
    List<long>? Assignees = null,
    int? RepeatAfter = null,
    string? RepeatMode = null,
    List<VikunjaReminder>? Reminders = null
);

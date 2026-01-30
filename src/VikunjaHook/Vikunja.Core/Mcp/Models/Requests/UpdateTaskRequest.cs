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
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? PercentDone = null,
    string? HexColor = null,
    int? RepeatAfter = null,
    string? RepeatMode = null,
    List<long>? Labels = null,
    List<long>? Assignees = null,
    List<DateTime>? Reminders = null
);

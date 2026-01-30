namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a Vikunja task
/// </summary>
public record VikunjaTask(
    long Id,
    string Title,
    string? Description,
    bool Done,
    DateTime? DoneAt,
    int Priority,
    DateTime? DueDate,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime Created,
    DateTime Updated,
    long ProjectId,
    int PercentDone,
    string? HexColor,
    List<VikunjaLabel>? Labels,
    List<VikunjaUser>? Assignees,
    List<VikunjaComment>? Comments,
    int? RepeatAfter,
    string? RepeatMode,
    List<VikunjaReminder>? Reminders,
    List<VikunjaTaskRelation>? RelatedTasks,
    VikunjaUser? CreatedBy,
    string? Identifier,
    int? Index,
    long? BucketId,
    double? Position,
    bool? IsFavorite
);

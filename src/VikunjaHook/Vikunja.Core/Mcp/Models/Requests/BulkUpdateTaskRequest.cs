using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request for bulk task update - done field
/// </summary>
public record BulkUpdateTaskDoneRequest(
    [property: JsonPropertyName("done")] bool Done
);

/// <summary>
/// Request for bulk task update - priority field
/// </summary>
public record BulkUpdateTaskPriorityRequest(
    [property: JsonPropertyName("priority")] int Priority
);

/// <summary>
/// Request for bulk task update - due date field
/// </summary>
public record BulkUpdateTaskDueDateRequest(
    [property: JsonPropertyName("due_date")] string? DueDate
);

/// <summary>
/// Request for bulk task update - project ID field
/// </summary>
public record BulkUpdateTaskProjectRequest(
    [property: JsonPropertyName("project_id")] long ProjectId
);

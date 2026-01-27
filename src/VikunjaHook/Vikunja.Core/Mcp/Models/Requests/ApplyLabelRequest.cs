namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to apply a label to a task
/// </summary>
public record ApplyLabelRequest(
    long TaskId,
    long LabelId
);

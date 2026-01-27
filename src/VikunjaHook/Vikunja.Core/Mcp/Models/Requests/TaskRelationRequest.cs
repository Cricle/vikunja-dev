using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to create a task relation
/// </summary>
public record TaskRelationRequest(
    [property: JsonPropertyName("task_id")] long TaskId,
    [property: JsonPropertyName("other_task_id")] long OtherTaskId,
    [property: JsonPropertyName("relation_kind")] string RelationKind
);

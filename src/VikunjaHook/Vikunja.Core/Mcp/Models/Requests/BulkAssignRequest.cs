using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to bulk assign users to a task
/// </summary>
public record BulkAssignRequest(
    [property: JsonPropertyName("user_ids")] List<long> UserIds
);

using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to add a comment to a task
/// </summary>
public record AddCommentRequest(
    [property: JsonPropertyName("comment")] string Comment
);

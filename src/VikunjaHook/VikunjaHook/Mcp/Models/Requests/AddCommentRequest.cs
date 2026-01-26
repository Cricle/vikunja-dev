using System.Text.Json.Serialization;

namespace VikunjaHook.Mcp.Models.Requests;

/// <summary>
/// Request to add a comment to a task
/// </summary>
public record AddCommentRequest(
    [property: JsonPropertyName("comment")] string Comment
);

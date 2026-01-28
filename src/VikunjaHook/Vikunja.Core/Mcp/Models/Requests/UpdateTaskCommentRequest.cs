using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

public sealed class UpdateTaskCommentRequest
{
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}

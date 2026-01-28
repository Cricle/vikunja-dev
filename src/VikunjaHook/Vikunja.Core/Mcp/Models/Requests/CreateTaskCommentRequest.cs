using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

public sealed class CreateTaskCommentRequest
{
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}

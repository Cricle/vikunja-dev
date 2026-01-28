using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

public sealed class AddTaskAssigneeRequest
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
}

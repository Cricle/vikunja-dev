using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

public sealed class AddTaskLabelRequest
{
    [JsonPropertyName("label_id")]
    public long LabelId { get; set; }
}

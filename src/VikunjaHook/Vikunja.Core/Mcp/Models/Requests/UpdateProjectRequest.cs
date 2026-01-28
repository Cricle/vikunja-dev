using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

public sealed class UpdateProjectRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("hex_color")]
    public string? HexColor { get; set; }
}

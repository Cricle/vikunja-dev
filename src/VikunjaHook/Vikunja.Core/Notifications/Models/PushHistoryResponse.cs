using System.Text.Json.Serialization;

namespace Vikunja.Core.Notifications.Models;

/// <summary>
/// Response for push history API
/// </summary>
public sealed class PushHistoryResponse
{
    [JsonPropertyName("records")]
    public required List<PushEventRecord> Records { get; init; }
    
    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }
}

/// <summary>
/// Response for clear history API
/// </summary>
public sealed class ClearHistoryResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

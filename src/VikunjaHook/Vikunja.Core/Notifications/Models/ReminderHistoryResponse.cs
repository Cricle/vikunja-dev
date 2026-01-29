using System.Text.Json.Serialization;

namespace Vikunja.Core.Notifications.Models;

public class ReminderHistoryResponse
{
    [JsonPropertyName("records")]
    public required List<TaskReminderRecord> Records { get; init; }
    
    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }
}

public class ReminderTestResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    
    [JsonPropertyName("count")]
    public required int Count { get; init; }
}

public class ReminderClearResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

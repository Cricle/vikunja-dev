using System.Text.Json.Serialization;

namespace Vikunja.Core.Notifications.Models;

/// <summary>
/// Record of a notification push attempt
/// </summary>
public sealed class PushEventRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("eventName")]
    public required string EventName { get; init; }
    
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }
    
    [JsonPropertyName("eventData")]
    public required object EventData { get; init; }
    
    [JsonPropertyName("providers")]
    public required List<ProviderPushResult> Providers { get; init; }
}

/// <summary>
/// Result of pushing to a specific provider
/// </summary>
public sealed class ProviderPushResult
{
    [JsonPropertyName("providerType")]
    public required string ProviderType { get; init; }
    
    [JsonPropertyName("success")]
    public required bool Success { get; init; }
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
    
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }
    
    [JsonPropertyName("notificationContent")]
    public NotificationMessage? NotificationContent { get; init; }
}

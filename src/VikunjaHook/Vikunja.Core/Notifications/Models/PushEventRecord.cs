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
    public required EventDataInfo EventData { get; init; }
    
    [JsonPropertyName("providers")]
    public required List<ProviderPushResult> Providers { get; init; }
}

/// <summary>
/// Event data information
/// </summary>
public sealed class EventDataInfo
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("body")]
    public required string Body { get; init; }
    
    [JsonPropertyName("format")]
    public required string Format { get; init; }
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

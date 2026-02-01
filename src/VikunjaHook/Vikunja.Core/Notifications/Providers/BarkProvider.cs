using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

public class BarkProvider : NotificationProviderBase
{
    private const string ApiBaseUrl = "https://api.day.app";

    private readonly HttpClient _httpClient;

    public override string ProviderType => "bark";

    public BarkProvider(
        HttpClient httpClient,
        ILogger<BarkProvider> logger) : base(logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Send notification with Bark device key
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        string? deviceKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
        {
            return CreateErrorResult("Bark device key is required");
        }

        return await SendWithRetryAsync(
            () => SendWithKeyAsync(message, deviceKey, cancellationToken),
            cancellationToken);
    }

    protected override async Task<NotificationResult> SendCoreAsync(
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        return CreateErrorResult("Bark requires device key. Use SendAsync(message, deviceKey) instead.");
    }

    private async Task<NotificationResult> SendWithKeyAsync(
        NotificationMessage message,
        string deviceKey,
        CancellationToken cancellationToken)
    {
        // URL encode title and body
        var encodedTitle = Uri.EscapeDataString(message.Title);
        var encodedBody = Uri.EscapeDataString(message.Body);
        
        // Bark API: GET https://api.day.app/{deviceKey}/{title}/{body}?isArchive=1
        var url = $"{ApiBaseUrl}/{deviceKey}/{encodedTitle}/{encodedBody}?isArchive=1";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CreateHttpErrorResult(response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync(
            BarkJsonContext.Default.BarkResponse,
            cancellationToken);

        return HandleApiResponse(
            result,
            r => r.Code == 200,
            r => r.Code,
            r => r.Message,
            "Bark notification sent successfully"
        );
    }

    protected override async Task<ValidationResult> ValidateConfigCoreAsync(
        ProviderConfig config,
        CancellationToken cancellationToken)
    {
        return await ValidateWithTestNotificationAsync(
            config,
            "deviceKey",
            "Bark device key",
            SendAsync,
            cancellationToken
        );
    }
}

public class BarkResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

// AOT-compatible JSON context for Bark
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BarkResponse))]
public partial class BarkJsonContext : JsonSerializerContext
{
}

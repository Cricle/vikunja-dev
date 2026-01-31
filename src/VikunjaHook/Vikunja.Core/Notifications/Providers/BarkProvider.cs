using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

public class BarkProvider : NotificationProviderBase
{
    private const string ApiBaseUrl = "https://api.day.app/push";

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
            return new NotificationResult(
                Success: false,
                ErrorMessage: "Bark device key is required",
                Timestamp: DateTime.UtcNow);
        }

        return await SendWithRetryAsync(
            () => SendWithKeyAsync(message, deviceKey, cancellationToken),
            cancellationToken);
    }

    protected override async Task<NotificationResult> SendCoreAsync(
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        return new NotificationResult(
            Success: false,
            ErrorMessage: "Bark requires device key. Use SendAsync(message, deviceKey) instead.",
            Timestamp: DateTime.UtcNow);
    }

    private async Task<NotificationResult> SendWithKeyAsync(
        NotificationMessage message,
        string deviceKey,
        CancellationToken cancellationToken)
    {
        var request = new BarkRequest
        {
            DeviceKey = deviceKey,
            Title = message.Title,
            Body = message.Body,
            // Bark supports markdown-like formatting in body
            IsArchive = 1 // Save to history
        };

        var response = await _httpClient.PostAsJsonAsync(
            ApiBaseUrl,
            request,
            BarkJsonContext.Default.BarkRequest,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync(
                BarkJsonContext.Default.BarkResponse,
                cancellationToken);

            if (result?.Code == 200)
            {
                Logger.LogInformation("Bark notification sent successfully");
                return new NotificationResult(
                    Success: true,
                    ErrorMessage: null,
                    Timestamp: DateTime.UtcNow);
            }
            else
            {
                var errorMsg = $"Bark API error: Code {result?.Code}";
                if (result?.Message != null)
                {
                    errorMsg += $" - {result.Message}";
                }
                return new NotificationResult(
                    Success: false,
                    ErrorMessage: errorMsg,
                    Timestamp: DateTime.UtcNow);
            }
        }
        else
        {
            return new NotificationResult(
                Success: false,
                ErrorMessage: $"HTTP error: {response.StatusCode}",
                Timestamp: DateTime.UtcNow);
        }
    }

    protected override async Task<ValidationResult> ValidateConfigCoreAsync(
        ProviderConfig config,
        CancellationToken cancellationToken)
    {
        if (!config.Settings.TryGetValue("deviceKey", out var deviceKey) ||
            string.IsNullOrWhiteSpace(deviceKey))
        {
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: "Bark device key is required");
        }

        // Test the device key by sending a test request
        try
        {
            var testMessage = new NotificationMessage(
                Title: "Test Notification",
                Body: "This is a test notification from Vikunja Webhook System",
                Format: NotificationFormat.Text);

            var result = await SendAsync(testMessage, deviceKey, cancellationToken);

            if (result.Success)
            {
                return new ValidationResult(IsValid: true);
            }
            else
            {
                return new ValidationResult(
                    IsValid: false,
                    ErrorMessage: $"Device key validation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating Bark configuration");
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"Validation error: {ex.Message}");
        }
    }
}

// Bark API models
public class BarkRequest
{
    [JsonPropertyName("device_key")]
    public string DeviceKey { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("isArchive")]
    public int IsArchive { get; set; } = 1;
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
[JsonSerializable(typeof(BarkRequest))]
[JsonSerializable(typeof(BarkResponse))]
public partial class BarkJsonContext : JsonSerializerContext
{
}

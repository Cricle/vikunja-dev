using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Interfaces;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

public class PushDeerProvider : INotificationProvider
{
    private const string ApiBaseUrl = "https://api2.pushdeer.com";
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 1000;
    private const double BackoffMultiplier = 2.0;

    private readonly HttpClient _httpClient;
    private readonly ILogger<PushDeerProvider> _logger;

    public string ProviderType => "pushdeer";

    public PushDeerProvider(
        HttpClient httpClient,
        ILogger<PushDeerProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(message, null, cancellationToken);
    }

    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        string? pushKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pushKey))
        {
            return new NotificationResult(
                Success: false,
                ErrorMessage: "PushDeer API key is required",
                Timestamp: DateTime.UtcNow);
        }

        var attempt = 0;
        var delay = InitialDelayMs;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;

            try
            {
                var request = new PushDeerRequest
                {
                    Pushkey = pushKey,
                    Text = message.Title,
                    Desp = message.Body,
                    Type = message.Format == NotificationFormat.Markdown ? "markdown" : "text"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{ApiBaseUrl}/message/push",
                    request,
                    PushDeerJsonContext.Default.PushDeerRequest,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync(
                        PushDeerJsonContext.Default.PushDeerResponse,
                        cancellationToken);

                    if (result?.Code == 0)
                    {
                        _logger.LogInformation("PushDeer notification sent successfully");
                        return new NotificationResult(
                            Success: true,
                            ErrorMessage: null,
                            Timestamp: DateTime.UtcNow);
                    }
                    else
                    {
                        var errorMsg = $"PushDeer API error: Code {result?.Code}";
                        if (result?.Error != null)
                        {
                            errorMsg += $" - {result.Error}";
                        }
                        _logger.LogWarning(errorMsg);
                        lastException = new Exception(errorMsg);
                    }
                }
                else
                {
                    var errorMsg = $"HTTP error: {response.StatusCode}";
                    _logger.LogWarning(errorMsg);
                    lastException = new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending PushDeer notification (attempt {Attempt}/{MaxRetries})",
                    attempt, MaxRetries);
                lastException = ex;
            }

            if (attempt < MaxRetries)
            {
                _logger.LogInformation("Retrying in {Delay}ms...", delay);
                await Task.Delay(delay, cancellationToken);
                delay = (int)(delay * BackoffMultiplier);
            }
        }

        return new NotificationResult(
            Success: false,
            ErrorMessage: $"Failed after {MaxRetries} attempts: {lastException?.Message}",
            Timestamp: DateTime.UtcNow);
    }

    public async Task<ValidationResult> ValidateConfigAsync(
        ProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.ProviderType != ProviderType)
        {
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"Invalid provider type: {config.ProviderType}");
        }

        if (!config.Settings.TryGetValue("pushkey", out var pushKey) ||
            string.IsNullOrWhiteSpace(pushKey))
        {
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: "PushDeer API key (pushkey) is required");
        }

        // Test the API key by sending a test request
        try
        {
            var testMessage = new NotificationMessage(
                Title: "Test Notification",
                Body: "This is a test notification from Vikunja Webhook System",
                Format: NotificationFormat.Text);

            var result = await SendAsync(testMessage, pushKey, cancellationToken);

            if (result.Success)
            {
                return new ValidationResult(IsValid: true);
            }
            else
            {
                return new ValidationResult(
                    IsValid: false,
                    ErrorMessage: $"API key validation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PushDeer configuration");
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"Validation error: {ex.Message}");
        }
    }
}

// PushDeer API models
public class PushDeerRequest
{
    [JsonPropertyName("pushkey")]
    public string Pushkey { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("desp")]
    public string Desp { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
}

public class PushDeerResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }  // Changed from string to object as API returns an object

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

// AOT-compatible JSON context for PushDeer
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PushDeerRequest))]
[JsonSerializable(typeof(PushDeerResponse))]
public partial class PushDeerJsonContext : JsonSerializerContext
{
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

public class PushDeerProvider : NotificationProviderBase
{
    private const string ApiBaseUrl = "https://api2.pushdeer.com";

    private readonly HttpClient _httpClient;

    public override string ProviderType => "pushdeer";

    public PushDeerProvider(
        HttpClient httpClient,
        ILogger<PushDeerProvider> logger) : base(logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Send notification with PushDeer API key
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        string? pushKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pushKey))
        {
            return CreateErrorResult("PushDeer API key is required");
        }

        return await SendWithRetryAsync(
            () => SendWithKeyAsync(message, pushKey, cancellationToken),
            cancellationToken);
    }

    protected override async Task<NotificationResult> SendCoreAsync(
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        return CreateErrorResult("PushDeer requires API key. Use SendAsync(message, pushKey) instead.");
    }

    private async Task<NotificationResult> SendWithKeyAsync(
        NotificationMessage message,
        string pushKey,
        CancellationToken cancellationToken)
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

        if (!response.IsSuccessStatusCode)
        {
            return CreateHttpErrorResult(response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync(
            PushDeerJsonContext.Default.PushDeerResponse,
            cancellationToken);

        return HandleApiResponse(
            result,
            r => r.Code == 0,
            r => r.Code,
            r => r.Error,
            "PushDeer notification sent successfully"
        );
    }

    protected override async Task<ValidationResult> ValidateConfigCoreAsync(
        ProviderConfig config,
        CancellationToken cancellationToken)
    {
        return await ValidateWithTestNotificationAsync(
            config,
            "pushkey",
            "PushDeer API key (pushkey)",
            SendAsync,
            cancellationToken
        );
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
    public object? Content { get; set; }

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

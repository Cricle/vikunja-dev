using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

public class BarkProvider : KeyBasedProviderBase
{
    private const string ApiBaseUrl = "https://api.day.app";
    private readonly HttpClient _httpClient;

    public override string ProviderType => "bark";
    protected override string KeySettingName => "deviceKey";
    protected override string KeyDisplayName => "Bark device key";

    public BarkProvider(HttpClient httpClient, ILogger<BarkProvider> logger) 
        : base(logger)
    {
        _httpClient = httpClient;
    }

    protected override async Task<NotificationResult> SendWithKeyAsync(
        NotificationMessage message,
        string deviceKey,
        CancellationToken cancellationToken)
    {
        var encodedTitle = Uri.EscapeDataString(message.Title);
        var encodedBody = Uri.EscapeDataString(message.Body);
        var url = $"{ApiBaseUrl}/{deviceKey}/{encodedTitle}/{encodedBody}?isArchive=1";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return CreateHttpErrorResult(response.StatusCode);

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

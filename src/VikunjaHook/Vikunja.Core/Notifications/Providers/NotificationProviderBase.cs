using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

/// <summary>
/// Base class for notification providers with built-in retry logic
/// </summary>
public abstract class NotificationProviderBase
{
    protected readonly ILogger Logger;
    
    protected virtual int MaxRetries => 3;
    protected virtual int InitialDelayMs => 1000;
    protected virtual double BackoffMultiplier => 2.0;

    public abstract string ProviderType { get; }

    protected NotificationProviderBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Send notification with automatic retry logic
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(
            () => SendCoreAsync(message, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Core send implementation to be provided by derived classes
    /// </summary>
    protected abstract Task<NotificationResult> SendCoreAsync(
        NotificationMessage message,
        CancellationToken cancellationToken);

    /// <summary>
    /// Execute an operation with exponential backoff retry logic
    /// </summary>
    protected async Task<NotificationResult> SendWithRetryAsync(
        Func<Task<NotificationResult>> operation,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        var delay = InitialDelayMs;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;

            try
            {
                var result = await operation();
                
                if (result.Success)
                {
                    return result;
                }

                // If not successful, treat as retriable error
                lastException = new Exception(result.ErrorMessage ?? "Unknown error");
                Logger.LogWarning("Notification failed (attempt {Attempt}/{MaxRetries}): {Error}",
                    attempt, MaxRetries, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending notification (attempt {Attempt}/{MaxRetries})",
                    attempt, MaxRetries);
                lastException = ex;
            }

            if (attempt < MaxRetries)
            {
                Logger.LogInformation("Retrying in {Delay}ms...", delay);
                await Task.Delay(delay, cancellationToken);
                delay = (int)(delay * BackoffMultiplier);
            }
        }

        return new NotificationResult(
            Success: false,
            ErrorMessage: $"Failed after {MaxRetries} attempts: {lastException?.Message}",
            Timestamp: DateTime.UtcNow);
    }

    /// <summary>
    /// Validate provider configuration
    /// </summary>
    public virtual async Task<ValidationResult> ValidateConfigAsync(
        ProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        if (config.ProviderType != ProviderType)
        {
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"Invalid provider type: {config.ProviderType}");
        }

        return await ValidateConfigCoreAsync(config, cancellationToken);
    }

    /// <summary>
    /// Core validation implementation to be provided by derived classes
    /// </summary>
    protected virtual Task<ValidationResult> ValidateConfigCoreAsync(
        ProviderConfig config,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ValidationResult(IsValid: true));
    }

    /// <summary>
    /// 通用的配置验证辅助方法：检查必需的设置键并发送测试通知
    /// </summary>
    protected async Task<ValidationResult> ValidateWithTestNotificationAsync(
        ProviderConfig config,
        string settingKey,
        string settingDisplayName,
        Func<NotificationMessage, string, CancellationToken, Task<NotificationResult>> sendFunc,
        CancellationToken cancellationToken)
    {
        // 检查必需的设置键
        if (!config.Settings.TryGetValue(settingKey, out var settingValue) ||
            string.IsNullOrWhiteSpace(settingValue))
        {
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"{settingDisplayName} is required");
        }

        // 发送测试通知
        try
        {
            var testMessage = new NotificationMessage(
                Title: "Test Notification",
                Body: "This is a test notification from Vikunja Webhook System",
                Format: NotificationFormat.Text);

            var result = await sendFunc(testMessage, settingValue, cancellationToken);

            if (result.Success)
            {
                return new ValidationResult(IsValid: true);
            }
            else
            {
                return new ValidationResult(
                    IsValid: false,
                    ErrorMessage: $"{settingDisplayName} validation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating {ProviderType} configuration", ProviderType);
            return new ValidationResult(
                IsValid: false,
                ErrorMessage: $"Validation error: {ex.Message}");
        }
    }
}

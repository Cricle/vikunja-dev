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
}

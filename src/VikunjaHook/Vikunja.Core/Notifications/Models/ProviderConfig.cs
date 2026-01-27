namespace Vikunja.Core.Notifications.Models;

public record ProviderConfig(
    string ProviderType,
    Dictionary<string, string> Settings)
{
    public ProviderConfig() : this(string.Empty, new Dictionary<string, string>())
    {
    }
}

public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null)
{
    public ValidationResult() : this(false, null)
    {
    }
}

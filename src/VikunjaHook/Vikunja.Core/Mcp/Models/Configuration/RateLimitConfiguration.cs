namespace Vikunja.Core.Mcp.Models.Configuration;

/// <summary>
/// Configuration for rate limiting
/// </summary>
public class RateLimitConfiguration
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
}

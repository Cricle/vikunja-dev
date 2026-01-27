namespace Vikunja.Core.Mcp.Models.Configuration;

/// <summary>
/// Configuration for Vikunja API connection
/// </summary>
public class VikunjaConfiguration
{
    public int DefaultTimeout { get; set; } = 30000;
}

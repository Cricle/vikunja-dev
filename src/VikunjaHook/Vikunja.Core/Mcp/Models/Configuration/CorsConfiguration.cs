namespace Vikunja.Core.Mcp.Models.Configuration;

/// <summary>
/// Configuration for CORS policy
/// </summary>
public class CorsConfiguration
{
    public string[] AllowedOrigins { get; set; } = new[] { "*" };
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE" };
    public string[] AllowedHeaders { get; set; } = new[] { "*" };
}

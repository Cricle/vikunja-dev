namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Authentication type for Vikunja API
/// </summary>
public enum AuthType
{
    /// <summary>
    /// API token authentication (tokens starting with "tk_")
    /// </summary>
    ApiToken,
    
    /// <summary>
    /// JWT token authentication (tokens starting with "eyJ")
    /// </summary>
    Jwt
}

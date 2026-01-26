using VikunjaHook.Mcp.Models.Configuration;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Service for validating configuration on startup
/// </summary>
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateConfiguration(IConfiguration configuration)
    {
        var isValid = true;

        // Validate Vikunja configuration
        var vikunjaConfig = configuration.GetSection("Vikunja").Get<VikunjaConfiguration>();
        if (vikunjaConfig != null)
        {
            if (vikunjaConfig.DefaultTimeout <= 0)
            {
                _logger.LogError("Vikunja:DefaultTimeout must be greater than 0");
                isValid = false;
            }
            else
            {
                _logger.LogInformation("Vikunja configuration validated: DefaultTimeout={Timeout}ms", vikunjaConfig.DefaultTimeout);
            }
        }

        // Validate MCP configuration
        var mcpConfig = configuration.GetSection("Mcp").Get<McpConfiguration>();
        if (mcpConfig != null)
        {
            if (string.IsNullOrWhiteSpace(mcpConfig.ServerName))
            {
                _logger.LogError("Mcp:ServerName cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(mcpConfig.Version))
            {
                _logger.LogError("Mcp:Version cannot be empty");
                isValid = false;
            }
            if (mcpConfig.MaxConcurrentConnections <= 0)
            {
                _logger.LogError("Mcp:MaxConcurrentConnections must be greater than 0");
                isValid = false;
            }
            
            if (isValid)
            {
                _logger.LogInformation("MCP configuration validated: ServerName={ServerName}, Version={Version}, MaxConnections={MaxConnections}",
                    mcpConfig.ServerName, mcpConfig.Version, mcpConfig.MaxConcurrentConnections);
            }
        }

        // Validate CORS configuration
        var corsConfig = configuration.GetSection("Cors").Get<CorsConfiguration>();
        if (corsConfig != null)
        {
            if (corsConfig.AllowedOrigins == null || corsConfig.AllowedOrigins.Length == 0)
            {
                _logger.LogWarning("Cors:AllowedOrigins is empty, using default value [*]");
            }
            else
            {
                _logger.LogInformation("CORS configuration validated: AllowedOrigins={Origins}", string.Join(", ", corsConfig.AllowedOrigins));
            }
        }

        // Validate Rate Limit configuration
        var rateLimitConfig = configuration.GetSection("RateLimit").Get<RateLimitConfiguration>();
        if (rateLimitConfig != null)
        {
            if (rateLimitConfig.Enabled)
            {
                if (rateLimitConfig.RequestsPerMinute <= 0)
                {
                    _logger.LogError("RateLimit:RequestsPerMinute must be greater than 0 when rate limiting is enabled");
                    isValid = false;
                }
                if (rateLimitConfig.RequestsPerHour <= 0)
                {
                    _logger.LogError("RateLimit:RequestsPerHour must be greater than 0 when rate limiting is enabled");
                    isValid = false;
                }
                
                if (isValid)
                {
                    _logger.LogInformation("Rate limit configuration validated: Enabled={Enabled}, RequestsPerMinute={PerMinute}, RequestsPerHour={PerHour}",
                        rateLimitConfig.Enabled, rateLimitConfig.RequestsPerMinute, rateLimitConfig.RequestsPerHour);
                }
            }
            else
            {
                _logger.LogWarning("Rate limiting is disabled");
            }
        }

        return isValid;
    }
}

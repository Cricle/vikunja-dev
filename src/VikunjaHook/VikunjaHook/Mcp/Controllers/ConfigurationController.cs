using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using VikunjaHook.Mcp.Models.Configuration;

namespace VikunjaHook.Mcp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationController> _logger;
    private readonly string _configFilePath;

    public ConfigurationController(
        IConfiguration configuration,
        ILogger<ConfigurationController> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _configFilePath = Path.Combine(environment.ContentRootPath, "appsettings.json");
    }

    [HttpGet]
    public IActionResult GetConfiguration()
    {
        try
        {
            var vikunjaConfig = new VikunjaConfiguration
            {
                DefaultTimeout = _configuration.GetValue<int>("Vikunja:DefaultTimeout", 30000)
            };
            
            var mcpConfig = new McpConfiguration
            {
                ServerName = _configuration.GetValue<string>("Mcp:ServerName") ?? "vikunja-mcp",
                Version = _configuration.GetValue<string>("Mcp:Version") ?? "1.0.0",
                MaxConcurrentConnections = _configuration.GetValue<int>("Mcp:MaxConcurrentConnections", 100)
            };
            
            var corsConfig = new CorsConfiguration
            {
                AllowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" },
                AllowedMethods = _configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE" },
                AllowedHeaders = _configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? new[] { "*" }
            };
            
            var rateLimitConfig = new RateLimitConfiguration
            {
                Enabled = _configuration.GetValue<bool>("RateLimit:Enabled", true),
                RequestsPerMinute = _configuration.GetValue<int>("RateLimit:RequestsPerMinute", 60),
                RequestsPerHour = _configuration.GetValue<int>("RateLimit:RequestsPerHour", 1000)
            };

            var response = new ConfigurationResponse
            {
                Vikunja = vikunjaConfig,
                Mcp = mcpConfig,
                Cors = corsConfig,
                RateLimit = rateLimitConfig
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading configuration");
            return StatusCode(500, new { error = "Failed to read configuration" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration([FromBody] ConfigurationUpdateRequest request)
    {
        try
        {
            // Read current configuration file
            if (!System.IO.File.Exists(_configFilePath))
            {
                return NotFound(new { error = "Configuration file not found" });
            }

            var jsonString = await System.IO.File.ReadAllTextAsync(_configFilePath);
            var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // Create a mutable dictionary from the JSON
            var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
            if (configDict == null)
            {
                return BadRequest(new { error = "Invalid configuration format" });
            }

            // Update Vikunja settings
            if (request.Vikunja != null)
            {
                var vikunjaDict = new Dictionary<string, object>
                {
                    ["DefaultTimeout"] = request.Vikunja.DefaultTimeout
                };
                configDict["Vikunja"] = JsonSerializer.SerializeToElement(vikunjaDict);
            }

            // Update MCP settings
            if (request.Mcp != null)
            {
                var mcpDict = new Dictionary<string, object>
                {
                    ["ServerName"] = request.Mcp.ServerName,
                    ["Version"] = request.Mcp.Version,
                    ["MaxConcurrentConnections"] = request.Mcp.MaxConcurrentConnections
                };
                configDict["Mcp"] = JsonSerializer.SerializeToElement(mcpDict);
            }

            // Update CORS settings
            if (request.Cors != null)
            {
                var corsDict = new Dictionary<string, object>
                {
                    ["AllowedOrigins"] = request.Cors.AllowedOrigins,
                    ["AllowedMethods"] = request.Cors.AllowedMethods,
                    ["AllowedHeaders"] = request.Cors.AllowedHeaders
                };
                configDict["Cors"] = JsonSerializer.SerializeToElement(corsDict);
            }

            // Update Rate Limit settings
            if (request.RateLimit != null)
            {
                var rateLimitDict = new Dictionary<string, object>
                {
                    ["Enabled"] = request.RateLimit.Enabled,
                    ["RequestsPerMinute"] = request.RateLimit.RequestsPerMinute,
                    ["RequestsPerHour"] = request.RateLimit.RequestsPerHour
                };
                configDict["RateLimit"] = JsonSerializer.SerializeToElement(rateLimitDict);
            }

            // Write back to file with formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var updatedJson = JsonSerializer.Serialize(configDict, options);
            await System.IO.File.WriteAllTextAsync(_configFilePath, updatedJson);

            _logger.LogInformation("Configuration updated successfully");

            return Ok(new { message = "Configuration updated successfully. Restart required for changes to take effect." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(500, new { error = "Failed to update configuration" });
        }
    }
}

public class ConfigurationUpdateRequest
{
    public VikunjaConfiguration? Vikunja { get; set; }
    public McpConfiguration? Mcp { get; set; }
    public CorsConfiguration? Cors { get; set; }
    public RateLimitConfiguration? RateLimit { get; set; }
}

public class ConfigurationResponse
{
    public VikunjaConfiguration Vikunja { get; set; } = new();
    public McpConfiguration Mcp { get; set; } = new();
    public CorsConfiguration Cors { get; set; } = new();
    public RateLimitConfiguration RateLimit { get; set; } = new();
}

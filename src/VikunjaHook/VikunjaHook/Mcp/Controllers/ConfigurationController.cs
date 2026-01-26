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
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationController(
        IConfiguration configuration,
        ILogger<ConfigurationController> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _configFilePath = Path.Combine(environment.ContentRootPath, "appsettings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            WriteIndented = true
        };
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
            
            // Parse as JsonDocument
            using var jsonDoc = JsonDocument.Parse(jsonString);
            
            // Build updated JSON manually to avoid AOT issues
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                
                // Copy existing properties and update as needed
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Name == "Vikunja" && request.Vikunja != null)
                    {
                        writer.WritePropertyName("Vikunja");
                        writer.WriteStartObject();
                        writer.WriteNumber("DefaultTimeout", request.Vikunja.DefaultTimeout);
                        writer.WriteEndObject();
                    }
                    else if (property.Name == "Mcp" && request.Mcp != null)
                    {
                        writer.WritePropertyName("Mcp");
                        writer.WriteStartObject();
                        writer.WriteString("ServerName", request.Mcp.ServerName);
                        writer.WriteString("Version", request.Mcp.Version);
                        writer.WriteNumber("MaxConcurrentConnections", request.Mcp.MaxConcurrentConnections);
                        writer.WriteEndObject();
                    }
                    else if (property.Name == "Cors" && request.Cors != null)
                    {
                        writer.WritePropertyName("Cors");
                        writer.WriteStartObject();
                        writer.WritePropertyName("AllowedOrigins");
                        writer.WriteStartArray();
                        foreach (var origin in request.Cors.AllowedOrigins)
                        {
                            writer.WriteStringValue(origin);
                        }
                        writer.WriteEndArray();
                        writer.WritePropertyName("AllowedMethods");
                        writer.WriteStartArray();
                        foreach (var method in request.Cors.AllowedMethods)
                        {
                            writer.WriteStringValue(method);
                        }
                        writer.WriteEndArray();
                        writer.WritePropertyName("AllowedHeaders");
                        writer.WriteStartArray();
                        foreach (var header in request.Cors.AllowedHeaders)
                        {
                            writer.WriteStringValue(header);
                        }
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                    else if (property.Name == "RateLimit" && request.RateLimit != null)
                    {
                        writer.WritePropertyName("RateLimit");
                        writer.WriteStartObject();
                        writer.WriteBoolean("Enabled", request.RateLimit.Enabled);
                        writer.WriteNumber("RequestsPerMinute", request.RateLimit.RequestsPerMinute);
                        writer.WriteNumber("RequestsPerHour", request.RateLimit.RequestsPerHour);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        // Copy existing property as-is
                        property.WriteTo(writer);
                    }
                }
                
                writer.WriteEndObject();
            }

            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Interfaces;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Configuration;

public class JsonFileConfigurationManager : IConfigurationManager
{
    private readonly string _configDirectory;
    private readonly ILogger<JsonFileConfigurationManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFileConfigurationManager(
        ILogger<JsonFileConfigurationManager> logger,
        string? configDirectory = null)
    {
        _logger = logger;
        _configDirectory = configDirectory ?? Path.Combine("data", "configs");
        
        // Ensure directory exists
        Directory.CreateDirectory(_configDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = WebhookNotificationJsonContext.Default
        };
    }

    public async Task<UserConfig?> LoadUserConfigAsync(
        string userId, 
        CancellationToken cancellationToken = default)
    {
        var filePath = GetConfigPath(userId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Configuration file not found for user {UserId}", userId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var config = JsonSerializer.Deserialize(json, WebhookNotificationJsonContext.Default.UserConfig);
            
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration for user {UserId}", userId);
                return GetDefaultConfig(userId);
            }
            
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corrupted configuration file for user {UserId}. Using default configuration.", userId);
            return GetDefaultConfig(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration for user {UserId}", userId);
            return GetDefaultConfig(userId);
        }
    }

    public async Task SaveUserConfigAsync(
        UserConfig config, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.UserId))
        {
            throw new ArgumentException("UserId cannot be empty", nameof(config));
        }

        // Validate configuration before saving
        if (!ValidateConfig(config))
        {
            throw new InvalidOperationException("Configuration validation failed");
        }

        config.LastModified = DateTime.UtcNow;
        
        var targetPath = GetConfigPath(config.UserId);
        var tempPath = $"{targetPath}.tmp";

        try
        {
            // Write to temp file first (atomic write)
            var json = JsonSerializer.Serialize(config, WebhookNotificationJsonContext.Default.UserConfig);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            
            // Atomic rename
            File.Move(tempPath, targetPath, overwrite: true);
            
            _logger.LogInformation("Configuration saved for user {UserId}", config.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration for user {UserId}", config.UserId);
            
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            throw;
        }
    }

    public async Task<IReadOnlyList<UserConfig>> LoadAllConfigsAsync(
        CancellationToken cancellationToken = default)
    {
        var configs = new List<UserConfig>();
        
        if (!Directory.Exists(_configDirectory))
        {
            _logger.LogWarning("Configuration directory does not exist: {Directory}", _configDirectory);
            return configs;
        }

        var configFiles = Directory.GetFiles(_configDirectory, "*.json");
        
        foreach (var filePath in configFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var config = await LoadUserConfigAsync(fileName, cancellationToken);
                
                if (config != null)
                {
                    configs.Add(config);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration file: {FilePath}", filePath);
                // Continue loading other configs
            }
        }
        
        _logger.LogInformation("Loaded {Count} user configurations", configs.Count);
        return configs;
    }

    public async Task<byte[]> ExportConfigsAsync(
        IEnumerable<string> userIds, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Export functionality will be implemented in task 9.1");
    }

    public async Task ImportConfigsAsync(
        byte[] zipData, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Import functionality will be implemented in task 9.2");
    }

    private string GetConfigPath(string userId)
    {
        // Sanitize userId to prevent path traversal
        var sanitizedUserId = string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_configDirectory, $"{sanitizedUserId}.json");
    }

    private bool ValidateConfig(UserConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.UserId))
        {
            _logger.LogWarning("Configuration validation failed: UserId is empty");
            return false;
        }

        // Validate provider configs
        foreach (var provider in config.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.ProviderType))
            {
                _logger.LogWarning("Configuration validation failed: Provider type is empty");
                return false;
            }
        }

        // Validate project rules
        foreach (var rule in config.ProjectRules)
        {
            if (string.IsNullOrWhiteSpace(rule.ProjectId))
            {
                _logger.LogWarning("Configuration validation failed: ProjectId is empty");
                return false;
            }
        }

        // Validate templates
        foreach (var template in config.Templates.Values)
        {
            if (string.IsNullOrWhiteSpace(template.EventType))
            {
                _logger.LogWarning("Configuration validation failed: Template EventType is empty");
                return false;
            }
        }

        return true;
    }

    private UserConfig GetDefaultConfig(string userId)
    {
        return new UserConfig
        {
            UserId = userId,
            Providers = new List<ProviderConfig>(),
            ProjectRules = new List<ProjectRule>
            {
                new ProjectRule
                {
                    ProjectId = "*",
                    EnabledEvents = new List<string>(EventTypes.All)
                }
            },
            Templates = new Dictionary<string, NotificationTemplate>(),
            LastModified = DateTime.UtcNow
        };
    }
}

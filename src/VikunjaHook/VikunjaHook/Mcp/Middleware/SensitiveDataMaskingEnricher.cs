using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace VikunjaHook.Mcp.Middleware;

/// <summary>
/// Serilog enricher that masks sensitive data in log messages
/// </summary>
public partial class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    // Regex patterns for sensitive data
    [GeneratedRegex(@"tk_[a-zA-Z0-9]{40,}", RegexOptions.Compiled)]
    private static partial Regex ApiTokenPattern();
    
    [GeneratedRegex(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtPattern();
    
    [GeneratedRegex(@"Bearer\s+[a-zA-Z0-9_-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenPattern();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.MessageTemplate.Text.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            logEvent.MessageTemplate.Text.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            logEvent.MessageTemplate.Text.Contains("bearer", StringComparison.OrdinalIgnoreCase))
        {
            // Mask sensitive data in message template properties
            var maskedProperties = new List<LogEventProperty>();
            
            foreach (var property in logEvent.Properties)
            {
                if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
                {
                    var maskedValue = MaskSensitiveData(stringValue);
                    if (maskedValue != stringValue)
                    {
                        maskedProperties.Add(new LogEventProperty(property.Key, new ScalarValue(maskedValue)));
                    }
                }
            }

            // Replace properties with masked versions
            foreach (var maskedProperty in maskedProperties)
            {
                logEvent.AddOrUpdateProperty(maskedProperty);
            }
        }
    }

    private static string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask API tokens (tk_...)
        input = ApiTokenPattern().Replace(input, match =>
        {
            var token = match.Value;
            return token.Length > 10 ? $"{token[..10]}...{token[^4..]}" : "tk_***";
        });

        // Mask JWT tokens
        input = JwtPattern().Replace(input, match =>
        {
            var token = match.Value;
            var parts = token.Split('.');
            return parts.Length == 3 ? $"{parts[0][..10]}...{parts[2][^4..]}" : "eyJ***";
        });

        // Mask Bearer tokens
        input = BearerTokenPattern().Replace(input, "Bearer ***");

        return input;
    }
}

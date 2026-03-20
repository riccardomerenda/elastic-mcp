using System.Text.Json;
using System.Text.RegularExpressions;
using ElasticMcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticMcp.Services;

public class SecurityGuard
{
    private readonly ElasticMcpOptions _options;
    private readonly ILogger<SecurityGuard> _logger;

    public SecurityGuard(IOptions<ElasticMcpOptions> options, ILogger<SecurityGuard> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Validates that the requested index is allowed by the allowlist/denylist configuration.
    /// Returns an error message if denied, or null if allowed.
    /// </summary>
    public string? ValidateIndexAccess(string index)
    {
        // Check denylist first — explicit denials take priority
        if (_options.DeniedIndices.Count > 0)
        {
            foreach (var pattern in _options.DeniedIndices)
            {
                if (MatchesPattern(index, pattern))
                {
                    _logger.LogWarning("Index access denied: {Index} matches deny pattern {Pattern}", index, pattern);
                    return $"Access denied: index '{index}' is blocked by deny pattern '{pattern}'.";
                }
            }
        }

        // Check allowlist — if configured, only listed patterns are permitted
        if (_options.AllowedIndices.Count > 0)
        {
            var allowed = _options.AllowedIndices.Any(pattern => MatchesPattern(index, pattern));
            if (!allowed)
            {
                _logger.LogWarning("Index access denied: {Index} not in allowlist", index);
                return $"Access denied: index '{index}' is not in the allowed indices list.";
            }
        }

        _logger.LogDebug("Index access granted: {Index}", index);
        return null;
    }

    /// <summary>
    /// Clamps the requested size to the configured maximum.
    /// </summary>
    public int ClampResultSize(int requestedSize)
    {
        return Math.Min(Math.Max(requestedSize, 1), _options.MaxResultSize);
    }

    /// <summary>
    /// Removes redacted fields from a JSON document.
    /// </summary>
    public JsonElement RedactFields(JsonElement document)
    {
        if (_options.RedactedFields.Count == 0)
            return document;

        if (document.ValueKind != JsonValueKind.Object)
            return document;

        var dict = new Dictionary<string, JsonElement>();

        foreach (var property in document.EnumerateObject())
        {
            if (_options.RedactedFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind == JsonValueKind.Object)
                dict[property.Name] = RedactFields(property.Value);
            else
                dict[property.Name] = property.Value;
        }

        return JsonSerializer.SerializeToElement(dict);
    }

    /// <summary>
    /// Removes redacted field names from a mapping properties dictionary.
    /// </summary>
    public Dictionary<string, object> RedactMappingProperties(Dictionary<string, object> properties)
    {
        if (_options.RedactedFields.Count == 0)
            return properties;

        return properties
            .Where(kvp => !_options.RedactedFields.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Logs an audit entry for a tool invocation.
    /// </summary>
    public void AuditToolCall(string toolName, string index, string? query = null)
    {
        _logger.LogInformation(
            "MCP Tool invoked: {Tool} on index {Index} with query {Query}",
            toolName, index, query ?? "(none)");
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        // Convert Elasticsearch-style glob patterns (e.g., "logs-*", ".security-*") to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}

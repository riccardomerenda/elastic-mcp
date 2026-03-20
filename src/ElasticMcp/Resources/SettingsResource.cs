using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Resources;

[McpServerResourceType]
public class SettingsResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://index/{name}/settings",
        Name = "Index Settings",
        MimeType = "application/json")]
    [Description("Settings of an Elasticsearch index including replicas, shards, analyzers, and refresh interval")]
    public static async Task<string> GetSettings(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The index name")] string name,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(name);
        if (accessError != null) return accessError;

        var response = await client.Indices.GetSettingsAsync(
            r => r.Indices(name),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Failed to retrieve settings: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var responseBytes = response.ApiCallDetails?.ResponseBodyInBytes;
        if (responseBytes != null)
            return JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonElement>(responseBytes),
                new JsonSerializerOptions { WriteIndented = true });

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }
}

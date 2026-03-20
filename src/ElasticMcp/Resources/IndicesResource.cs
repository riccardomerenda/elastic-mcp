using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ModelContextProtocol.Server;

namespace ElasticMcp.Resources;

[McpServerResourceType]
public class IndicesResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://cluster/indices",
        Name = "Cluster Indices",
        MimeType = "application/json")]
    [Description("List of all Elasticsearch indices with document count, size, status, and health")]
    public static async Task<string> GetIndices(
        ElasticsearchClient client,
        CancellationToken cancellationToken = default)
    {
        // Use indices stats API (Cat API removed in ES client v9)
        var response = await client.Indices.StatsAsync(
            r => r.Indices("*"),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Failed to retrieve indices: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var indices = response.Indices?.Select(kvp => new
        {
            index = kvp.Key,
            docsCount = kvp.Value.Primaries?.Docs?.Count,
            storeSize = kvp.Value.Primaries?.Store?.SizeInBytes,
            totalShards = kvp.Value.Total?.Docs?.Count
        }) ?? [];

        return JsonSerializer.Serialize(new { indices }, new JsonSerializerOptions { WriteIndented = true });
    }
}

using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ModelContextProtocol.Server;

namespace ElasticMcp.Resources;

[McpServerResourceType]
public class ClusterHealthResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://cluster/health",
        Name = "Cluster Health",
        MimeType = "application/json")]
    [Description("Elasticsearch cluster health status including status color, node count, and shard information")]
    public static async Task<string> GetClusterHealth(
        ElasticsearchClient client,
        CancellationToken cancellationToken = default)
    {
        var response = await client.Cluster.HealthAsync(cancellationToken: cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Failed to retrieve cluster health: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var result = new
        {
            status = response.Status.ToString(),
            clusterName = response.ClusterName,
            numberOfNodes = response.NumberOfNodes,
            numberOfDataNodes = response.NumberOfDataNodes,
            activeShards = response.ActiveShards,
            activePrimaryShards = response.ActivePrimaryShards,
            relocatingShards = response.RelocatingShards,
            initializingShards = response.InitializingShards,
            unassignedShards = response.UnassignedShards,
            activeShardsPercentAsNumber = response.ActiveShardsPercentAsNumber
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Resources;

[McpServerResourceType]
public class SampleResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://index/{name}/sample",
        Name = "Index Sample",
        MimeType = "application/json")]
    [Description("A sample of documents from an Elasticsearch index to understand its data structure")]
    public static async Task<string> GetSample(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The index name")] string name,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(name);
        if (accessError != null) return accessError;

        var response = await client.SearchAsync<JsonElement>(s => s
            .Indices(name)
            .Size(5)
            .Query(q => q.MatchAll(_ => {})),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Failed to retrieve sample: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var samples = response.Hits.Select(h => new
        {
            id = h.Id,
            source = guard.RedactFields(h.Source)
        });

        var result = new
        {
            index = name,
            sample_size = response.Documents.Count,
            documents = samples
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class SearchTool
{
    [McpServerTool(Name = "search")]
    [Description("Perform a full-text search on an Elasticsearch index. Returns matching documents with scores.")]
    public static async Task<string> Search(
        ElasticsearchClient client,
        IOptions<ElasticMcpOptions> options,
        [Description("The Elasticsearch index name or pattern (e.g. 'logs-*')")] string index,
        [Description("The search query string (Lucene syntax)")] string query,
        [Description("Number of results to return (default: 10)")] int size = 10,
        [Description("Offset for pagination (default: 0)")] int from = 0,
        CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        size = Math.Min(size, config.MaxResultSize);

        var response = await client.SearchAsync<JsonElement>(s => s
            .Indices(index)
            .From(from)
            .Size(size)
            .Query(q => q
                .QueryString(qs => qs.Query(query))
            ),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var result = new
        {
            total = response.Total,
            returned = response.Documents.Count,
            hits = response.Hits.Select(h => new
            {
                id = h.Id,
                index = h.Index,
                score = h.Score,
                source = h.Source
            })
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

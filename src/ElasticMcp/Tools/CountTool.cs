using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class CountTool
{
    [McpServerTool(Name = "count")]
    [Description("Count documents in an Elasticsearch index, optionally filtered by a query.")]
    public static async Task<string> Count(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The Elasticsearch index name or pattern")] string index,
        [Description("Optional query string to filter documents (default: match all)")] string? query = null,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return accessError;

        guard.AuditToolCall("count", index, query);

        var response = await client.CountAsync(c =>
        {
            c.Indices(index);
            if (!string.IsNullOrWhiteSpace(query))
            {
                c.Query(q => q.QueryString(qs => qs.Query(query)));
            }
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Count failed: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var result = new
        {
            index,
            count = response.Count,
            query = query ?? "(match_all)"
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

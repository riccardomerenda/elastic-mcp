using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class ExplainQueryTool
{
    [McpServerTool(Name = "explain_query")]
    [Description("Show the Elasticsearch Query DSL that would be generated for a search, without executing it. Useful for debugging and learning.")]
    public static Task<string> ExplainQuery(
        SecurityGuard guard,
        [Description("The Elasticsearch index name or pattern")] string index,
        [Description("The search query string")] string query,
        [Description("Number of results (default: 10)")] int size = 10,
        [Description("Offset for pagination (default: 0)")] int from = 0)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return Task.FromResult(accessError);

        guard.AuditToolCall("explain_query", index, query);

        size = guard.ClampResultSize(size);

        var searchBody = new
        {
            size,
            from,
            query = new
            {
                query_string = new
                {
                    query
                }
            }
        };

        var result = new
        {
            description = $"Query DSL for: \"{query}\" on index \"{index}\"",
            endpoint = $"GET /{index}/_search",
            body = searchBody
        };

        return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}

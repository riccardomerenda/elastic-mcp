using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Configuration;
using ElasticMcp.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class SemanticSearchTool
{
    [McpServerTool(Name = "semantic_search")]
    [Description("Perform a semantic (vector) search using kNN on a dense_vector field. " +
                 "Pass a query vector (float array) to find the most similar documents.")]
    public static async Task<string> SemanticSearch(
        ElasticsearchClient client,
        SecurityGuard guard,
        IOptions<ElasticMcpOptions> options,
        [Description("The Elasticsearch index name or pattern")] string index,
        [Description("The query vector as a JSON array of floats, e.g. [0.1, 0.2, ...]")] float[] query_vector,
        [Description("The dense_vector field to search (default from config)")] string? vector_field = null,
        [Description("Number of nearest neighbors to return (default from config)")] int? k = null,
        [Description("Number of candidates to consider (default from config)")] int? num_candidates = null,
        [Description("Optional query string to pre-filter documents before kNN")] string? filter_query = null,
        [Description("Minimum similarity threshold (optional)")] float? similarity = null,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return accessError;

        var opts = options.Value;
        var resolvedField = vector_field ?? opts.SemanticSearch.DefaultVectorField;
        var resolvedK = k ?? opts.SemanticSearch.DefaultK;
        var resolvedCandidates = num_candidates ?? opts.SemanticSearch.DefaultNumCandidates;
        var clampedSize = guard.ClampResultSize(resolvedK);

        guard.AuditToolCall("semantic_search", index,
            $"knn field={resolvedField} k={resolvedK} vector_dims={query_vector.Length}");

        var response = await client.SearchAsync<JsonElement>(s =>
        {
            s.Indices(index)
             .Size(clampedSize)
             .Knn(knn =>
             {
                 knn.Field(resolvedField)
                    .QueryVector(query_vector)
                    .K(resolvedK)
                    .NumCandidates(resolvedCandidates);

                 if (similarity.HasValue)
                     knn.Similarity(similarity.Value);

                 if (!string.IsNullOrWhiteSpace(filter_query))
                     knn.Filter(q => q.QueryString(qs => qs.Query(filter_query)));
             });
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Semantic search failed: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var result = new
        {
            total = response.Total,
            returned = response.Documents.Count,
            vector_field = resolvedField,
            k = resolvedK,
            hits = response.Hits.Select(h => new
            {
                id = h.Id,
                index = h.Index,
                score = h.Score,
                source = guard.RedactFields(h.Source)
            })
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
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
        [Description("The Elasticsearch index name or pattern")] string index,
        [Description("The query vector as a JSON array of floats, e.g. '[0.1, 0.2, 0.3]'")] string query_vector,
        [Description("The dense_vector field to search (default: 'embedding')")] string? vector_field = null,
        [Description("Number of nearest neighbors to return (default: 10)")] int? k = null,
        [Description("Number of candidates to consider (default: 100)")] int? num_candidates = null,
        [Description("Optional query string to pre-filter documents before kNN")] string? filter_query = null,
        [Description("Minimum similarity threshold (optional)")] float? similarity = null,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return accessError;

        // Parse the query vector from JSON string
        float[] vectorArray;
        try
        {
            vectorArray = JsonSerializer.Deserialize<float[]>(query_vector)
                ?? throw new JsonException("Parsed vector is null");
        }
        catch (JsonException)
        {
            return "Error: query_vector must be a valid JSON array of floats, e.g. '[0.1, 0.2, 0.3]'";
        }

        var config = guard.SemanticSearchConfig;
        var resolvedField = vector_field ?? config.DefaultVectorField;
        var resolvedK = k ?? config.DefaultK;
        var resolvedCandidates = num_candidates ?? config.DefaultNumCandidates;
        var clampedSize = guard.ClampResultSize(resolvedK);

        guard.AuditToolCall("semantic_search", index,
            $"knn field={resolvedField} k={resolvedK} vector_dims={vectorArray.Length}");

        var response = await client.SearchAsync<JsonElement>(s =>
        {
            s.Indices(index)
             .Size(clampedSize)
             .Knn(knn =>
             {
                 knn.Field(resolvedField)
                    .QueryVector(vectorArray)
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

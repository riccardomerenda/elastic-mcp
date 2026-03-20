using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class GetDocumentTool
{
    [McpServerTool(Name = "get_document")]
    [Description("Retrieve a single document from Elasticsearch by its ID.")]
    public static async Task<string> GetDocument(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The Elasticsearch index name")] string index,
        [Description("The document ID")] string document_id,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return accessError;

        guard.AuditToolCall("get_document", index, $"id:{document_id}");

        var response = await client.GetAsync<JsonElement>(index, document_id, cancellationToken: cancellationToken);

        if (!response.IsValidResponse)
        {
            if (response.ApiCallDetails?.HttpStatusCode == 404)
                return $"Document not found: index='{index}', id='{document_id}'";

            return $"Get document failed: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var source = response.Source;
        if (guard != null)
            source = guard.RedactFields(source);

        var result = new
        {
            id = response.Id,
            index = response.Index,
            version = response.Version,
            found = response.Found,
            source
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

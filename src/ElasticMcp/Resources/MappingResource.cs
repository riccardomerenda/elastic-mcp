using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Resources;

[McpServerResourceType]
public class MappingResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://index/{name}/mapping",
        Name = "Index Mapping",
        MimeType = "application/json")]
    [Description("Schema/mapping of an Elasticsearch index including field names, types, and analyzers")]
    public static async Task<string> GetMapping(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The index name")] string name,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(name);
        if (accessError != null) return accessError;

        // Use a raw search to get field mappings via the field_caps API instead,
        // which gives us field names and types without serialization issues
        var response = await client.FieldCapsAsync(
            r => r.Indices(name).Fields("*"),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Failed to retrieve mapping: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        var fields = response.Fields?
            .Select(kvp => new
            {
                field = kvp.Key,
                types = kvp.Value.Select(t => new
                {
                    type = t.Key,
                    searchable = t.Value.Searchable,
                    aggregatable = t.Value.Aggregatable
                })
            });

        var result = new
        {
            index = name,
            fields
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class GetDocumentToolTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public GetDocumentToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task GetDocument_ExistingId_ReturnsDocument()
    {
        var result = await GetDocumentTool.GetDocument(
            _fixture.Client, _guard, "test-logs", "1");

        Assert.Contains("\"found\": true", result);
        Assert.Contains("Test Document 1", result);
    }

    [Fact]
    public async Task GetDocument_NonExistentId_ReturnsNotFound()
    {
        var result = await GetDocumentTool.GetDocument(
            _fixture.Client, _guard, "test-logs", "nonexistent");

        Assert.Contains("not found", result.ToLowerInvariant());
    }

    [Fact]
    public async Task GetDocument_RedactsFields()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { RedactedFields = ["message"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await GetDocumentTool.GetDocument(
            _fixture.Client, guard, "test-logs", "1");

        Assert.Contains("\"found\": true", result);
        Assert.DoesNotContain("\"message\"", result);
    }
}

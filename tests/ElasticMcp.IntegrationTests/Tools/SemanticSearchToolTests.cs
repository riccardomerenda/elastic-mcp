using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class SemanticSearchToolTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public SemanticSearchToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task SemanticSearch_FindsNearestNeighbors()
    {
        // Vector close to "cats" document [1, 0, 0]
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard,
            "test-vectors", "[1.0, 0.0, 0.0]",
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("\"vector_field\": \"embedding\"", result);
        Assert.Contains("Document about cats", result);
    }

    [Fact]
    public async Task SemanticSearch_RespectsK()
    {
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard,
            "test-vectors", "[1.0, 0.0, 0.0]",
            vector_field: "embedding", k: 1, num_candidates: 10);

        Assert.Contains("\"returned\": 1", result);
    }

    [Fact]
    public async Task SemanticSearch_DeniedIndex_ReturnsError()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { DeniedIndices = ["test-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, guard,
            "test-vectors", "[1.0, 0.0, 0.0]",
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("Access denied", result);
    }

    [Fact]
    public async Task SemanticSearch_NonExistentIndex_ReturnsError()
    {
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard,
            "nonexistent-index", "[1.0, 0.0, 0.0]",
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("failed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task SemanticSearch_UsesConfigDefaults()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions
            {
                SemanticSearch = new SemanticSearchOptions
                {
                    DefaultVectorField = "embedding",
                    DefaultK = 2,
                    DefaultNumCandidates = 10
                }
            }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, guard,
            "test-vectors", "[0.0, 1.0, 0.0]");

        Assert.Contains("\"k\": 2", result);
        Assert.Contains("\"vector_field\": \"embedding\"", result);
    }

    [Fact]
    public async Task SemanticSearch_InvalidVector_ReturnsError()
    {
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard,
            "test-vectors", "not a vector",
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("query_vector must be a valid JSON array", result);
    }
}

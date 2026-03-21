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
    private readonly IOptions<ElasticMcpOptions> _options;

    public SemanticSearchToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
        _options = Options.Create(new ElasticMcpOptions());
    }

    [Fact]
    public async Task SemanticSearch_FindsNearestNeighbors()
    {
        // Vector close to "cats" document [1, 0, 0]
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard, _options,
            "test-vectors", [1.0f, 0.0f, 0.0f],
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("\"vector_field\": \"embedding\"", result);
        Assert.Contains("Document about cats", result);
    }

    [Fact]
    public async Task SemanticSearch_RespectsK()
    {
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard, _options,
            "test-vectors", [1.0f, 0.0f, 0.0f],
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
            _fixture.Client, guard, _options,
            "test-vectors", [1.0f, 0.0f, 0.0f],
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("Access denied", result);
    }

    [Fact]
    public async Task SemanticSearch_NonExistentIndex_ReturnsError()
    {
        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard, _options,
            "nonexistent-index", [1.0f, 0.0f, 0.0f],
            vector_field: "embedding", k: 3, num_candidates: 10);

        Assert.Contains("failed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task SemanticSearch_UsesConfigDefaults()
    {
        var options = Options.Create(new ElasticMcpOptions
        {
            SemanticSearch = new SemanticSearchOptions
            {
                DefaultVectorField = "embedding",
                DefaultK = 2,
                DefaultNumCandidates = 10
            }
        });

        var result = await SemanticSearchTool.SemanticSearch(
            _fixture.Client, _guard, options,
            "test-vectors", [0.0f, 1.0f, 0.0f]);

        Assert.Contains("\"k\": 2", result);
        Assert.Contains("\"vector_field\": \"embedding\"", result);
    }
}

using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class AggregateToolTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public AggregateToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task Aggregate_Terms_ReturnsBuckets()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "terms", "level.keyword");

        Assert.Contains("\"aggregation_type\": \"terms\"", result);
        Assert.Contains("\"total_docs\"", result);
    }

    [Fact]
    public async Task Aggregate_Cardinality_ReturnsValue()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "cardinality", "level.keyword");

        Assert.Contains("\"aggregation_type\": \"cardinality\"", result);
    }

    [Fact]
    public async Task Aggregate_WithQuery_FiltersBeforeAggregating()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "terms", "level.keyword", query: "level:error");

        Assert.Contains("\"aggregation_type\": \"terms\"", result);
    }

    [Fact]
    public async Task Aggregate_DeniedIndex_ReturnsError()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { DeniedIndices = ["test-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await AggregateTool.Aggregate(
            _fixture.Client, guard, "test-logs", "terms", "level.keyword");

        Assert.Contains("Access denied", result);
    }
}

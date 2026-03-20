using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class AggregateToolEdgeCaseTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public AggregateToolEdgeCaseTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task Aggregate_DateHistogram_ReturnsBuckets()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "date_histogram", "timestamp", interval: "1d");

        Assert.Contains("\"aggregation_type\": \"date_histogram\"", result);
        Assert.Contains("\"total_docs\"", result);
    }

    [Fact]
    public async Task Aggregate_Avg_ReturnsValue()
    {
        // avg on a non-numeric field won't crash, but may return null
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "avg", "timestamp");

        Assert.Contains("\"aggregation_type\": \"avg\"", result);
    }

    [Fact]
    public async Task Aggregate_Min_ReturnsValue()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "min", "timestamp");

        Assert.Contains("\"aggregation_type\": \"min\"", result);
    }

    [Fact]
    public async Task Aggregate_Max_ReturnsValue()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "max", "timestamp");

        Assert.Contains("\"aggregation_type\": \"max\"", result);
    }

    [Fact]
    public async Task Aggregate_Sum_ReturnsValue()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "test-logs", "sum", "timestamp");

        Assert.Contains("\"aggregation_type\": \"sum\"", result);
    }

    [Fact]
    public async Task Aggregate_UnsupportedType_ThrowsError()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            AggregateTool.Aggregate(
                _fixture.Client, _guard, "test-logs", "unsupported_type", "level.keyword"));
    }

    [Fact]
    public async Task Aggregate_NonExistentIndex_ReturnsError()
    {
        var result = await AggregateTool.Aggregate(
            _fixture.Client, _guard, "nonexistent-index", "terms", "level.keyword");

        Assert.Contains("failed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task Aggregate_AllowedListRejects()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { AllowedIndices = ["prod-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await AggregateTool.Aggregate(
            _fixture.Client, guard, "test-logs", "terms", "level.keyword");

        Assert.Contains("Access denied", result);
    }
}

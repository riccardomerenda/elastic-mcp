using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class CountToolTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public CountToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { MaxResultSize = 100 }),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task Count_AllDocuments_Returns5()
    {
        var result = await CountTool.Count(_fixture.Client, _guard, "test-logs");

        Assert.Contains("\"count\": 5", result);
    }

    [Fact]
    public async Task Count_WithQuery_FiltersDocuments()
    {
        var result = await CountTool.Count(_fixture.Client, _guard, "test-logs", "level:error");

        Assert.Contains("\"count\": 2", result);
    }

    [Fact]
    public async Task Count_NoQuery_ReturnsMatchAll()
    {
        var result = await CountTool.Count(_fixture.Client, _guard, "test-logs");

        Assert.Contains("\"query\": \"(match_all)\"", result);
    }
}

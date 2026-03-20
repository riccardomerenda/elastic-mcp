using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Tools;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class CountToolTests
{
    private readonly ElasticsearchFixture _fixture;

    public CountToolTests(ElasticsearchFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Count_AllDocuments_Returns5()
    {
        var result = await CountTool.Count(_fixture.Client, "test-logs");

        Assert.Contains("\"count\": 5", result);
    }

    [Fact]
    public async Task Count_WithQuery_FiltersDocuments()
    {
        var result = await CountTool.Count(_fixture.Client, "test-logs", "level:error");

        Assert.Contains("\"count\": 2", result);
    }

    [Fact]
    public async Task Count_NoQuery_ReturnsMatchAll()
    {
        var result = await CountTool.Count(_fixture.Client, "test-logs");

        Assert.Contains("\"query\": \"(match_all)\"", result);
    }
}

using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class SearchToolTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public SearchToolTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { MaxResultSize = 100 }),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task Search_WithMatchAllQuery_ReturnsDocuments()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "test-logs", "*");

        Assert.Contains("Test Document", result);
        Assert.Contains("\"total\"", result);
    }

    [Fact]
    public async Task Search_RespectsMaxSize()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "test-logs", "*", size: 2);

        Assert.Contains("\"returned\": 2", result);
    }

    [Fact]
    public async Task Search_WithSpecificQuery_FiltersResults()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "test-logs", "level:error");

        Assert.Contains("Test Document", result);
        Assert.DoesNotContain("\"total\": 5", result);
    }
}

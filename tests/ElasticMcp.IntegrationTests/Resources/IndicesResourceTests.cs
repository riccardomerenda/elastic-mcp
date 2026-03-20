using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Resources;

namespace ElasticMcp.IntegrationTests.Resources;

[Collection("Elasticsearch")]
public class IndicesResourceTests
{
    private readonly ElasticsearchFixture _fixture;

    public IndicesResourceTests(ElasticsearchFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetIndices_ContainsTestLogsIndex()
    {
        var result = await IndicesResource.GetIndices(_fixture.Client);

        Assert.Contains("test-logs", result);
    }

    [Fact]
    public async Task GetIndices_ContainsDocCount()
    {
        var result = await IndicesResource.GetIndices(_fixture.Client);

        Assert.Contains("\"docsCount\"", result);
    }
}

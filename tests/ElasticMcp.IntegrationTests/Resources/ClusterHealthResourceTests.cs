using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Resources;

namespace ElasticMcp.IntegrationTests.Resources;

[Collection("Elasticsearch")]
public class ClusterHealthResourceTests
{
    private readonly ElasticsearchFixture _fixture;

    public ClusterHealthResourceTests(ElasticsearchFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetClusterHealth_ReturnsValidStatus()
    {
        var result = await ClusterHealthResource.GetClusterHealth(_fixture.Client);

        Assert.Contains("\"status\"", result);
        Assert.True(result.Contains("Green") || result.Contains("Yellow") || result.Contains("green") || result.Contains("yellow"),
            $"Expected green or yellow status, got: {result}");
    }

    [Fact]
    public async Task GetClusterHealth_ContainsNodeCount()
    {
        var result = await ClusterHealthResource.GetClusterHealth(_fixture.Client);

        Assert.Contains("\"numberOfNodes\"", result);
    }
}

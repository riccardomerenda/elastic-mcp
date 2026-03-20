using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Resources;
using ElasticMcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Resources;

[Collection("Elasticsearch")]
public class MappingResourceTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public MappingResourceTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task GetMapping_ReturnsFieldTypes()
    {
        var result = await MappingResource.GetMapping(
            _fixture.Client, _guard, "test-logs");

        Assert.Contains("test-logs", result);
    }
}

using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Resources;
using ElasticMcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Resources;

[Collection("Elasticsearch")]
public class SampleResourceTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public SampleResourceTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task GetSample_ReturnsSampleDocuments()
    {
        var result = await SampleResource.GetSample(
            _fixture.Client, _guard, "test-logs");

        Assert.Contains("\"sample_size\"", result);
        Assert.Contains("Test Document", result);
    }

    [Fact]
    public async Task GetSample_RedactsFields()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { RedactedFields = ["message"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SampleResource.GetSample(
            _fixture.Client, guard, "test-logs");

        Assert.Contains("Test Document", result); // title is not redacted
        Assert.DoesNotContain("\"message\"", result);
    }
}

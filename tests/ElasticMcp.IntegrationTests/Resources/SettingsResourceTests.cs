using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Resources;
using ElasticMcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Resources;

[Collection("Elasticsearch")]
public class SettingsResourceTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public SettingsResourceTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions()),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task GetSettings_ReturnsSettings()
    {
        var result = await SettingsResource.GetSettings(
            _fixture.Client, _guard, "test-logs");

        Assert.Contains("test-logs", result);
    }

    [Fact]
    public async Task GetSettings_DeniedIndex_ReturnsError()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { DeniedIndices = ["test-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SettingsResource.GetSettings(
            _fixture.Client, guard, "test-logs");

        Assert.Contains("Access denied", result);
    }
}

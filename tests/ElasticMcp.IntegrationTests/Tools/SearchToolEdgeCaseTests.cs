using ElasticMcp.Configuration;
using ElasticMcp.IntegrationTests.Fixtures;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

[Collection("Elasticsearch")]
public class SearchToolEdgeCaseTests
{
    private readonly ElasticsearchFixture _fixture;
    private readonly SecurityGuard _guard;

    public SearchToolEdgeCaseTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { MaxResultSize = 100 }),
            NullLogger<SecurityGuard>.Instance);
    }

    [Fact]
    public async Task Search_WithPagination_ReturnsOffset()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "test-logs", "*", size: 2, from: 2);

        Assert.Contains("\"returned\": 2", result);
    }

    [Fact]
    public async Task Search_NonExistentIndex_ReturnsError()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "nonexistent-index", "*");

        Assert.Contains("failed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task Search_DeniedIndex_ReturnsAccessDenied()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { DeniedIndices = [".security-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SearchTool.Search(
            _fixture.Client, guard, ".security-7", "*");

        Assert.Contains("Access denied", result);
    }

    [Fact]
    public async Task Search_AllowedListRejectsUnlisted()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { AllowedIndices = ["allowed-*"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SearchTool.Search(
            _fixture.Client, guard, "test-logs", "*");

        Assert.Contains("Access denied", result);
    }

    [Fact]
    public async Task Search_SizeClampedToMax()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { MaxResultSize = 3 }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SearchTool.Search(
            _fixture.Client, guard, "test-logs", "*", size: 100);

        Assert.Contains("\"returned\": 3", result);
    }

    [Fact]
    public async Task Search_RedactsFieldsFromResults()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { RedactedFields = ["level"] }),
            NullLogger<SecurityGuard>.Instance);

        var result = await SearchTool.Search(
            _fixture.Client, guard, "test-logs", "*");

        Assert.DoesNotContain("\"level\"", result);
        Assert.Contains("\"title\"", result);
    }

    [Fact]
    public async Task Search_EmptyResult_ReturnsZero()
    {
        var result = await SearchTool.Search(
            _fixture.Client, _guard, "test-logs", "nonexistent_field:impossible_value");

        Assert.Contains("\"total\": 0", result);
        Assert.Contains("\"returned\": 0", result);
    }
}

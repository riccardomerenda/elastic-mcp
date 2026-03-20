using ElasticMcp.Configuration;
using ElasticMcp.Services;
using ElasticMcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.IntegrationTests.Tools;

public class ExplainQueryToolTests
{
    private readonly SecurityGuard _guard = new(
        Options.Create(new ElasticMcpOptions()),
        NullLogger<SecurityGuard>.Instance);

    [Fact]
    public async Task ExplainQuery_ReturnsQueryDsl()
    {
        var result = await ExplainQueryTool.ExplainQuery(
            _guard, "test-logs", "level:error");

        Assert.Contains("\"query_string\"", result);
        Assert.Contains("level:error", result);
        Assert.Contains("GET /test-logs/_search", result);
    }

    [Fact]
    public async Task ExplainQuery_ClampsSizeToMax()
    {
        var guard = new SecurityGuard(
            Options.Create(new ElasticMcpOptions { MaxResultSize = 50 }),
            NullLogger<SecurityGuard>.Instance);

        var result = await ExplainQueryTool.ExplainQuery(
            guard, "test-logs", "*", size: 200);

        Assert.Contains("\"size\": 50", result);
    }
}

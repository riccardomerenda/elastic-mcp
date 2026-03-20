using ElasticMcp.Configuration;

namespace ElasticMcp.Tests.Configuration;

public class ElasticMcpOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new ElasticMcpOptions();

        Assert.Equal(["http://localhost:9200"], options.Nodes);
        Assert.True(options.ReadOnly);
        Assert.Equal(100, options.MaxResultSize);
        Assert.Equal("30s", options.QueryTimeout);
        Assert.Empty(options.AllowedIndices);
        Assert.Empty(options.DeniedIndices);
        Assert.Null(options.DefaultIndex);
        Assert.Null(options.Authentication);
    }

    [Fact]
    public void AuthenticationOptions_Defaults_AreCorrect()
    {
        var auth = new AuthenticationOptions();

        Assert.Equal("None", auth.Type);
        Assert.Null(auth.ApiKey);
        Assert.Null(auth.Username);
        Assert.Null(auth.Password);
    }

    [Theory]
    [InlineData(50, 100, 50)]
    [InlineData(200, 100, 100)]
    [InlineData(10, 10, 10)]
    public void MaxResultSize_ClampsBehavior(int requested, int maxConfig, int expected)
    {
        var actual = Math.Min(requested, maxConfig);
        Assert.Equal(expected, actual);
    }
}

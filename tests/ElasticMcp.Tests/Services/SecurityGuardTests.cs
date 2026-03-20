using System.Text.Json;
using ElasticMcp.Configuration;
using ElasticMcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElasticMcp.Tests.Services;

public class SecurityGuardTests
{
    private static SecurityGuard CreateGuard(ElasticMcpOptions? options = null) =>
        new(Options.Create(options ?? new ElasticMcpOptions()), NullLogger<SecurityGuard>.Instance);

    [Fact]
    public void ValidateIndexAccess_NoRestrictions_AllowsAll()
    {
        var guard = CreateGuard();
        Assert.Null(guard.ValidateIndexAccess("any-index"));
    }

    [Fact]
    public void ValidateIndexAccess_DenyListBlocks()
    {
        var guard = CreateGuard(new ElasticMcpOptions { DeniedIndices = [".security-*"] });

        Assert.NotNull(guard.ValidateIndexAccess(".security-7"));
        Assert.Contains("Access denied", guard.ValidateIndexAccess(".security-7"));
    }

    [Fact]
    public void ValidateIndexAccess_DenyListAllowsNonMatching()
    {
        var guard = CreateGuard(new ElasticMcpOptions { DeniedIndices = [".security-*"] });

        Assert.Null(guard.ValidateIndexAccess("logs-prod"));
    }

    [Fact]
    public void ValidateIndexAccess_AllowListOnlyPermitsMatching()
    {
        var guard = CreateGuard(new ElasticMcpOptions { AllowedIndices = ["logs-*", "docs-*"] });

        Assert.Null(guard.ValidateIndexAccess("logs-prod"));
        Assert.Null(guard.ValidateIndexAccess("docs-archive"));
        Assert.NotNull(guard.ValidateIndexAccess("users"));
    }

    [Fact]
    public void ValidateIndexAccess_DenyTakesPriorityOverAllow()
    {
        var guard = CreateGuard(new ElasticMcpOptions
        {
            AllowedIndices = ["logs-*"],
            DeniedIndices = ["logs-secret"]
        });

        Assert.Null(guard.ValidateIndexAccess("logs-prod"));
        Assert.NotNull(guard.ValidateIndexAccess("logs-secret"));
    }

    [Theory]
    [InlineData(50, 100, 50)]
    [InlineData(200, 100, 100)]
    [InlineData(0, 100, 1)]
    [InlineData(-5, 100, 1)]
    public void ClampResultSize_ClampsCorrectly(int requested, int max, int expected)
    {
        var guard = CreateGuard(new ElasticMcpOptions { MaxResultSize = max });
        Assert.Equal(expected, guard.ClampResultSize(requested));
    }

    [Fact]
    public void RedactFields_RemovesSpecifiedFields()
    {
        var guard = CreateGuard(new ElasticMcpOptions { RedactedFields = ["password", "ssn"] });

        var doc = JsonSerializer.SerializeToElement(new
        {
            name = "John",
            password = "secret",
            ssn = "123-45-6789",
            email = "john@example.com"
        });

        var redacted = guard.RedactFields(doc);
        var json = redacted.ToString();

        Assert.Contains("name", json);
        Assert.Contains("email", json);
        Assert.DoesNotContain("password", json);
        Assert.DoesNotContain("ssn", json);
    }

    [Fact]
    public void RedactFields_NoRedactedFields_ReturnsOriginal()
    {
        var guard = CreateGuard();

        var doc = JsonSerializer.SerializeToElement(new { name = "John", password = "secret" });
        var redacted = guard.RedactFields(doc);

        Assert.Contains("password", redacted.ToString());
    }

    [Fact]
    public void RedactFields_CaseInsensitive()
    {
        var guard = CreateGuard(new ElasticMcpOptions { RedactedFields = ["Password"] });

        var doc = JsonSerializer.SerializeToElement(new { name = "John", password = "secret" });
        var redacted = guard.RedactFields(doc);

        Assert.DoesNotContain("password", redacted.ToString());
    }
}

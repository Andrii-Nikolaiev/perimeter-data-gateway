using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Application.Resources;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.UnitTests.Resources;

public sealed class SalesSummaryRequestValidatorTests
{
    [Fact]
    public void Validate_WithoutParameters_ReturnsRequestWithoutCountry()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var result = validator.Validate(
            token,
            "SalesSummary",
            EmptyParameters());

        Assert.Same(token, result.Token);
        Assert.Equal("SalesSummary", result.ResourceName);
        Assert.Null(result.Country);
    }

    [Fact]
    public void Validate_WithSingleCountry_ReturnsNormalizedCountry()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["country"] = new[] { " Germany " }
            };

        var result = validator.Validate(
            token,
            "SalesSummary",
            parameters);

        Assert.Equal("Germany", result.Country);
    }

    [Fact]
    public void Validate_WithUnknownParameter_ThrowsInvalidRequest()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["sql"] = new[] { "SELECT 1" }
            };

        var exception = Assert.Throws<PdgException>(
            () => validator.Validate(
                token,
                "SalesSummary",
                parameters));

        Assert.Equal(
            PdgErrorCategory.InvalidRequest,
            exception.Category);
    }

    [Fact]
    public void Validate_WithDuplicateCountry_ThrowsInvalidRequest()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["country"] = new[]
                {
                    "Germany",
                    "France"
                }
            };

        var exception = Assert.Throws<PdgException>(
            () => validator.Validate(
                token,
                "SalesSummary",
                parameters));

        Assert.Equal(
            PdgErrorCategory.InvalidRequest,
            exception.Category);
    }

    [Fact]
    public void Validate_WithEmptyCountry_ThrowsInvalidRequest()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["country"] = new[] { string.Empty }
            };

        var exception = Assert.Throws<PdgException>(
            () => validator.Validate(
                token,
                "SalesSummary",
                parameters));

        Assert.Equal(
            PdgErrorCategory.InvalidRequest,
            exception.Category);
    }

    [Fact]
    public void Validate_WithWhitespaceCountry_ThrowsInvalidRequest()
    {
        var validator = new SalesSummaryRequestValidator();
        var token = CreateToken();

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["country"] = new[] { "   " }
            };

        var exception = Assert.Throws<PdgException>(
            () => validator.Validate(
                token,
                "SalesSummary",
                parameters));

        Assert.Equal(
            PdgErrorCategory.InvalidRequest,
            exception.Category);
    }

    private static ValidatedTokenContext CreateToken()
    {
        return new ValidatedTokenContext(
            "user_42",
            "sales_copilot_v1",
            new HashSet<string>(
                new[] { "sales.read" },
                StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>
        EmptyParameters()
    {
        return new Dictionary<string, IReadOnlyList<string>>();
    }
}
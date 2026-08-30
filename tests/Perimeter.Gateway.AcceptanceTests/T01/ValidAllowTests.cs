using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T01;

[Collection(AcceptanceCollection.Name)]
public sealed class ValidAllowTests
{
    private static readonly HashSet<string> EuropeCountries =
        new(
            new[]
            {
                "Austria",
                "Belgium",
                "Czech Republic",
                "Denmark",
                "Finland",
                "France",
                "Germany",
                "Hungary",
                "Ireland",
                "Italy",
                "Netherlands",
                "Norway",
                "Poland",
                "Portugal",
                "Spain",
                "Sweden",
                "United Kingdom"
            },
            StringComparer.Ordinal);

    private readonly AcceptanceEnvironment _environment;

    public ValidAllowTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T01_Valid_allow_returns_only_permitted_rows_and_writes_allow_audit()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var auditAssertions =
            new AuditAssertions(_environment);

        var baselineAuditId =
            await auditAssertions.GetLatestAuditIdAsync(ct);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenFactory.CreateValidToken());

        using var response =
            await _environment.Client.SendAsync(
                request,
                ct);

        var body =
            await ResponseAssertions.AssertSuccessAsync(
                response,
                ct);

        Assert.NotEmpty(body.Data);

        Assert.All(
            body.Data,
            row =>
            {
                Assert.Contains(
                    row.Country,
                    EuropeCountries);
            });

        var audit =
            await auditAssertions.GetLatestAfterAsync(
                baselineAuditId,
                "user_42",
                ct);

        AuditAssertions.AssertRequiredFields(audit);
        AuditAssertions.AssertNoSensitivePayload(audit);

        Assert.Equal(
            "user_42",
            audit.SubjectId);

        Assert.Equal(
            "sales_copilot_v1",
            audit.ActorId);

        Assert.Equal(
            "sales.read",
            audit.Capability);

        Assert.Equal(
            "SalesSummary",
            audit.ResourceName);

        Assert.Equal(
            "ALLOW",
            audit.Decision);

        Assert.Equal(
            body.Meta.RowsReturned,
            audit.RowsReturned);
    }
}

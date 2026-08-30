using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T18;

[Collection(AcceptanceCollection.Name)]
public sealed class CorporateDataSourceUnavailableTests
{
    private readonly AcceptanceEnvironment _environment;

    public CorporateDataSourceUnavailableTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T18_Corporate_data_source_failure_returns_503_and_writes_failure_audit()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var auditAssertions =
            new AuditAssertions(
                _environment);

        var baselineAuditId =
            await auditAssertions.GetLatestAuditIdAsync(ct);

        await _environment
            .CorporateData
            .Container
            .StopAsync(ct);

        try
        {
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

            await ResponseAssertions.AssertErrorAsync(
                response,
                HttpStatusCode.ServiceUnavailable,
                "corporate_data_source_unavailable",
                ct);

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
                "sales.read",
                audit.Scope);

            Assert.Equal(
                "ALLOW",
                audit.Decision);

            Assert.Equal(
                "corporate_data_source_unavailable",
                audit.ReasonCategory);

            Assert.Equal(
                0,
                audit.RowsReturned);
        }
        finally
        {
            await _environment
                .CorporateData
                .Container
                .StartAsync(ct);

            DatabaseConnectionPoolReset.ClearAll();
            _environment.RecreateWebHost();
        }
    }
}
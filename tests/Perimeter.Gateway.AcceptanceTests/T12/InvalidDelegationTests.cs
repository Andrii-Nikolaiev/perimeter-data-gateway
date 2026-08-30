using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T12;

[Collection(AcceptanceCollection.Name)]
public sealed class InvalidDelegationTests
{
    private readonly AcceptanceEnvironment _environment;

    public InvalidDelegationTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T12_Inactive_delegation_returns_403_without_reading_corporate_data()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var databaseMutator =
            new AcceptanceDatabaseMutator(
                _environment);

        var auditAssertions =
            new AuditAssertions(
                _environment);

        var baselineAuditId =
            await auditAssertions.GetLatestAuditIdAsync(ct);

        await databaseMutator.DisableDelegationAsync(ct);

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
                HttpStatusCode.Forbidden,
                "access_denied",
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
                "DENY",
                audit.Decision);

            Assert.Equal(
                0,
                audit.RowsReturned);
        }
        finally
        {
            try
            {
                await _environment
                    .CorporateData
                    .Container
                    .StartAsync(ct);

                DatabaseConnectionPoolReset.ClearAll();
                _environment.RecreateWebHost();
            }
            finally
            {
                await databaseMutator
                    .RestoreDelegationAsync(ct);
            }
        }
    }
}
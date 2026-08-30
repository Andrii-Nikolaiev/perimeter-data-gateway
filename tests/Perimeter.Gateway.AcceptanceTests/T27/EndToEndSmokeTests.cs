using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T27;

[Collection(AcceptanceCollection.Name)]
public sealed class EndToEndSmokeTests
{
    private readonly AcceptanceEnvironment _environment;

    public EndToEndSmokeTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T27_Startup_auth_allow_deny_and_audit_complete_end_to_end()
    {
        var ct =
            TestContext.Current.CancellationToken;

        using (var readinessResponse =
            await _environment.Client.GetAsync(
                "/health/ready",
                ct))
        {
            Assert.Equal(
                HttpStatusCode.OK,
                readinessResponse.StatusCode);
        }

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var auditAssertions =
            new AuditAssertions(
                _environment);

        var allowBaselineAuditId =
            await auditAssertions.GetLatestAuditIdAsync(ct);

        using (var allowRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary"))
        {
            allowRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokenFactory.CreateValidToken(
                        subjectId: "user_42"));

            using var allowResponse =
                await _environment.Client.SendAsync(
                    allowRequest,
                    ct);

            var allowBody =
                await ResponseAssertions.AssertSuccessAsync(
                    allowResponse,
                    ct);

            Assert.NotEmpty(
                allowBody.Data);

            var allowAudit =
                await auditAssertions.GetLatestAfterAsync(
                    allowBaselineAuditId,
                    "user_42",
                    ct);

            AuditAssertions.AssertRequiredFields(
                allowAudit);

            AuditAssertions.AssertNoSensitivePayload(
                allowAudit);

            Assert.Equal(
                "ALLOW",
                allowAudit.Decision);

            Assert.Equal(
                allowBody.Meta.RowsReturned,
                allowAudit.RowsReturned);
        }

        var denyBaselineAuditId =
            await auditAssertions.GetLatestAuditIdAsync(ct);

        using (var denyRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary"))
        {
            denyRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokenFactory.CreateValidToken(
                        subjectId: "user_44"));

            using var denyResponse =
                await _environment.Client.SendAsync(
                    denyRequest,
                    ct);

            await ResponseAssertions.AssertErrorAsync(
                denyResponse,
                HttpStatusCode.Forbidden,
                "access_denied",
                ct);

            var denyAudit =
                await auditAssertions.GetLatestAfterAsync(
                    denyBaselineAuditId,
                    "user_44",
                    ct);

            AuditAssertions.AssertRequiredFields(
                denyAudit);

            AuditAssertions.AssertNoSensitivePayload(
                denyAudit);

            Assert.Equal(
                "DENY",
                denyAudit.Decision);

            Assert.Equal(
                0,
                denyAudit.RowsReturned);
        }
    }
}
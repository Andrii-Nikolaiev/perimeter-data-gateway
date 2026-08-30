using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T20;

[Collection(AcceptanceCollection.Name)]
public sealed class AuditWriteFailureTests
{
    private readonly AcceptanceEnvironment _environment;

    public AuditWriteFailureTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T20_Audit_write_failure_returns_503_for_allow_and_deny()
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

        await databaseMutator.RevokeAuditInsertAsync(ct);

        try
        {
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

                await ResponseAssertions.AssertErrorAsync(
                    allowResponse,
                    HttpStatusCode.ServiceUnavailable,
                    "audit_write_failed",
                    ct);
            }

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
                    HttpStatusCode.ServiceUnavailable,
                    "audit_write_failed",
                    ct);
            }
        }
        finally
        {
            await databaseMutator.RestoreAuditInsertAsync(ct);
        }
    }
}
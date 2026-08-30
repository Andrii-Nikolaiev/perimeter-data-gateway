using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T13;

[Collection(AcceptanceCollection.Name)]
public sealed class ActorCapabilityAbsentTests
{
    private readonly AcceptanceEnvironment _environment;

    public ActorCapabilityAbsentTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T13_Missing_sales_read_scope_returns_403_and_writes_deny_audit()
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

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenFactory.CreateMissingScopeToken());

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

        AuditAssertions.AssertRequiredFields(
            audit,
            requireScope: false);

        Assert.True(
            string.IsNullOrWhiteSpace(audit.Scope));

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
}
using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T10;

[Collection(AcceptanceCollection.Name)]
public sealed class ExpiredJwtTests
{
    private readonly AcceptanceEnvironment _environment;

    public ExpiredJwtTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T10_Expired_jwt_returns_401_authentication_failed()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenFactory.CreateExpiredToken());

        using var response =
            await _environment.Client.SendAsync(
                request,
                ct);

        await ResponseAssertions.AssertErrorAsync(
            response,
            HttpStatusCode.Unauthorized,
            "authentication_failed",
            ct);
    }
}
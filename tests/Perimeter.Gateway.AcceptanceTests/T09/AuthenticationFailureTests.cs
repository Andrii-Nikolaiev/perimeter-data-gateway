using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T09;

[Collection(AcceptanceCollection.Name)]
public sealed class AuthenticationFailureTests
{
    private readonly AcceptanceEnvironment _environment;

    public AuthenticationFailureTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T09_Missing_or_invalid_jwt_returns_401_authentication_failed()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        await AssertAuthenticationFailedAsync(
            token: null,
            ct);

        var invalidSigningKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

        await AssertAuthenticationFailedAsync(
            tokenFactory.CreateToken(
                subjectId: "user_42",
                actorId: "sales_copilot_v1",
                scope: "sales.read",
                expires: DateTimeOffset.UtcNow.AddMinutes(5),
                signingKeyOverride: invalidSigningKey),
            ct);

        await AssertAuthenticationFailedAsync(
            tokenFactory.CreateToken(
                subjectId: "user_42",
                actorId: "sales_copilot_v1",
                scope: "sales.read",
                expires: DateTimeOffset.UtcNow.AddMinutes(5),
                issuerOverride:
                    "https://pdg.local/wrong-issuer"),
            ct);

        await AssertAuthenticationFailedAsync(
            tokenFactory.CreateToken(
                subjectId: "user_42",
                actorId: "sales_copilot_v1",
                scope: "sales.read",
                expires: DateTimeOffset.UtcNow.AddMinutes(5),
                audienceOverride:
                    "wrong-audience"),
            ct);
    }

    private async Task AssertAuthenticationFailedAsync(
        string? token,
        CancellationToken ct)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/resources/SalesSummary");

        if (token is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

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
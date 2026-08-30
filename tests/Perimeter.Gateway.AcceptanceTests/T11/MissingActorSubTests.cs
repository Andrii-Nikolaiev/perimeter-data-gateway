using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T11;

[Collection(AcceptanceCollection.Name)]
public sealed class MissingActorSubTests
{
    private readonly AcceptanceEnvironment _environment;

    public MissingActorSubTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T11_Missing_actor_sub_returns_401_without_subject_only_fallback()
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
                tokenFactory.CreateMissingActorToken());

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
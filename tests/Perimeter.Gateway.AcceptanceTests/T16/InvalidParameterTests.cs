using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T16;

[Collection(AcceptanceCollection.Name)]
public sealed class InvalidParameterTests
{
    private readonly AcceptanceEnvironment _environment;

    public InvalidParameterTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T16_Invalid_query_parameters_return_400_invalid_request()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var token =
            tokenFactory.CreateValidToken();

        var requestUris =
            new[]
            {
                "/api/resources/SalesSummary?unknown=value",
                "/api/resources/SalesSummary?country=Germany&country=France",
                "/api/resources/SalesSummary?country="
            };

        foreach (var requestUri in requestUris)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);

            using var response =
                await _environment.Client.SendAsync(
                    request,
                    ct);

            await ResponseAssertions.AssertErrorAsync(
                response,
                HttpStatusCode.BadRequest,
                "invalid_request",
                ct);
        }
    }
}
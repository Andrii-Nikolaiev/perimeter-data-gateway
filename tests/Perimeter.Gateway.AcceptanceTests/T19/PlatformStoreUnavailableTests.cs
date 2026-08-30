using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T19;

[Collection(AcceptanceCollection.Name)]
public sealed class PlatformStoreUnavailableTests
{
    private readonly AcceptanceEnvironment _environment;

    public PlatformStoreUnavailableTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T19_Platform_store_failure_returns_503_without_reading_corporate_data()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        await _environment
            .CorporateData
            .Container
            .StopAsync(ct);

        try
        {
            await _environment
                .PlatformStore
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
                    "platform_store_unavailable",
                    ct);
            }
            finally
            {
                await _environment
                    .PlatformStore
                    .Container
                    .StartAsync(ct);

                DatabaseConnectionPoolReset.ClearAll();
            }
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
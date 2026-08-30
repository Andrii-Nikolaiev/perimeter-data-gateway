using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T22;

[Collection(AcceptanceCollection.Name)]
public sealed class SqlInjectionAttemptTests
{
    private readonly AcceptanceEnvironment _environment;

    public SqlInjectionAttemptTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T22_Injection_looking_country_is_treated_as_bound_data()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        const string injectionLookingCountry =
            "Germany' OR '1'='1";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/resources/SalesSummary?country={Uri.EscapeDataString(injectionLookingCountry)}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenFactory.CreateValidToken(
                    subjectId: "user_43"));

        using var response =
            await _environment.Client.SendAsync(
                request,
                ct);

        var body =
            await ResponseAssertions.AssertSuccessAsync(
                response,
                ct);

        Assert.Empty(body.Data);
        Assert.Equal(
            0,
            body.Meta.RowsReturned);
    }
}
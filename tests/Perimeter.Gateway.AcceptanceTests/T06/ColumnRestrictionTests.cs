using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T06;

[Collection(AcceptanceCollection.Name)]
public sealed class ColumnRestrictionTests
{
    private static readonly HashSet<string> ForbiddenFields =
        new(
            new[]
            {
                "Address",
                "PostalCode",
                "Phone",
                "Fax",
                "Email"
            },
            StringComparer.OrdinalIgnoreCase);

    private readonly AcceptanceEnvironment _environment;

    public ColumnRestrictionTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T06_Success_response_does_not_expose_forbidden_columns()
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
                tokenFactory.CreateValidToken());

        using var response =
            await _environment.Client.SendAsync(
                request,
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content.ReadAsStringAsync(ct);

        using var document =
            JsonDocument.Parse(json);

        Assert.True(
            document.RootElement.TryGetProperty(
                "data",
                out var data));

        Assert.Equal(
            JsonValueKind.Array,
            data.ValueKind);

        Assert.True(
            data.GetArrayLength() > 0);

        foreach (var row in data.EnumerateArray())
        {
            Assert.Equal(
                JsonValueKind.Object,
                row.ValueKind);

            foreach (var property in row.EnumerateObject())
            {
                Assert.False(
                    ForbiddenFields.Contains(
                        property.Name),
                    $"Forbidden field '{property.Name}' was exposed.");
            }
        }
    }
}
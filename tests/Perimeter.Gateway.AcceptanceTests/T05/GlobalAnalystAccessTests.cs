using System.Net.Http.Headers;
using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T05;

[Collection(AcceptanceCollection.Name)]
public sealed class GlobalAnalystAccessTests
{
    private static readonly HashSet<string> EuropeCountries =
        new(
            new[]
            {
                "Austria",
                "Belgium",
                "Czech Republic",
                "Denmark",
                "Finland",
                "France",
                "Germany",
                "Hungary",
                "Ireland",
                "Italy",
                "Netherlands",
                "Norway",
                "Poland",
                "Portugal",
                "Spain",
                "Sweden",
                "United Kingdom"
            },
            StringComparer.Ordinal);

    private readonly AcceptanceEnvironment _environment;

    public GlobalAnalystAccessTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T05_Global_analyst_returns_all_standard_sales_summary_rows()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var expectedRowCount =
            await GetSourceRowCountAsync(ct);

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

        Assert.Equal(
            expectedRowCount,
            body.Data.Count);

        Assert.Contains(
            body.Data,
            row =>
                !EuropeCountries.Contains(
                    row.Country));
    }

    private async Task<int> GetSourceRowCountAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _environment
                    .CorporateData
                    .OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT COUNT(*)
                FROM pdg.sales_summary;
                """,
                connection);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(ct));
    }
}
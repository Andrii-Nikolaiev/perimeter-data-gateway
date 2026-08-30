using System.Net.Http.Headers;
using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T03;

[Collection(AcceptanceCollection.Name)]
public sealed class AutomaticRowFilteringTests
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

    public AutomaticRowFilteringTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T03_No_country_parameter_applies_automatic_europe_row_scope()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var sourceCounts =
            await GetSourceCountsAsync(ct);

        Assert.True(
            sourceCounts.OutOfScope > 0,
            "Corporate source must contain rows outside the Europe scope.");

        Assert.True(
            sourceCounts.InScope > 0,
            "Corporate source must contain rows inside the Europe scope.");

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

        var body =
            await ResponseAssertions.AssertSuccessAsync(
                response,
                ct);

        Assert.Equal(
            (int)sourceCounts.InScope,
            body.Data.Count);

        Assert.All(
            body.Data,
            row =>
            {
                Assert.Contains(
                    row.Country,
                    EuropeCountries);
            });
    }

    private async Task<SourceCounts> GetSourceCountsAsync(
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
                SELECT
                    COUNT(*) FILTER (
                        WHERE "Country" = ANY(@countries)
                    ),
                    COUNT(*) FILTER (
                        WHERE NOT ("Country" = ANY(@countries))
                    )
                FROM pdg.sales_summary;
                """,
                connection);

        command.Parameters.AddWithValue(
            "countries",
            EuropeCountries.ToArray());

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new SourceCounts(
            reader.GetInt64(0),
            reader.GetInt64(1));
    }

    private sealed record SourceCounts(
        long InScope,
        long OutOfScope);
}
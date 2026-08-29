using Npgsql;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;
using Perimeter.Gateway.Infrastructure.CorporateData;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.CorporateData;

public sealed class CorporateDataReaderTests
    : IClassFixture<CorporateDataIntegrationFixture>
{
    private readonly CorporateDataIntegrationFixture _fixture;

    public CorporateDataReaderTests(
        CorporateDataIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Reader_executes_fixed_all_and_allow_list_queries()
    {
        var ct = TestContext.Current.CancellationToken;

        var reader =
            new CorporateDataReader(
                _fixture.RuntimeConnectionString);

        var allScope =
            new RowScope(
                RowScopeMode.All,
                new Dictionary<string, IReadOnlySet<string>>());

        var allRows =
            await reader.ReadSalesSummaryAsync(
                allScope,
                3,
                ct);

        Assert.Equal(3, allRows.Count);

        var allowListScope =
            new RowScope(
                RowScopeMode.AllowList,
                new Dictionary<string, IReadOnlySet<string>>
                {
                    ["country"] =
                        new HashSet<string>(
                            new[] { "France" },
                            StringComparer.Ordinal)
                });

        var franceRows =
            await reader.ReadSalesSummaryAsync(
                allowListScope,
                500,
                ct);

        Assert.NotEmpty(franceRows);
        Assert.All(
            franceRows,
            row => Assert.Equal("France", row.Country));
        Assert.True(franceRows.Count <= 500);
    }

    [Fact]
    public async Task Reader_maps_database_failure_to_corporate_data_source_unavailable()
    {
        var ct = TestContext.Current.CancellationToken;

        var connectionString =
            new NpgsqlConnectionStringBuilder(
                _fixture.RuntimeConnectionString)
            {
                Host = "127.0.0.1",
                Port = 1,
                Pooling = false,
                Timeout = 1
            }
            .ConnectionString;

        var reader =
            new CorporateDataReader(connectionString);

        var scope =
            new RowScope(
                RowScopeMode.All,
                new Dictionary<string, IReadOnlySet<string>>());

        var exception =
            await Assert.ThrowsAsync<PdgException>(
                () => reader.ReadSalesSummaryAsync(
                    scope,
                    1,
                    ct));

        Assert.Equal(
            PdgErrorCategory.CorporateDataSourceUnavailable,
            exception.Category);

        Assert.NotNull(exception.InnerException);
    }
}

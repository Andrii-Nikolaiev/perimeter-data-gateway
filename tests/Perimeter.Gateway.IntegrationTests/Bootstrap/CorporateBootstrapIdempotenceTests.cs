using Npgsql;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.Bootstrap;

public sealed class CorporateBootstrapIdempotenceTests
    : IClassFixture<CorporateDataIntegrationFixture>
{
    private readonly CorporateDataIntegrationFixture _fixture;

    public CorporateBootstrapIdempotenceTests(
        CorporateDataIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repeated_corporate_bootstrap_preserves_desired_state()
    {
        var ct = TestContext.Current.CancellationToken;

        var before =
            await ReadCorporateStateAsync(ct);

        Assert.Equal(11L, before.BaseTables);
        Assert.True(before.SalesSummaryRows > 0);

        await _fixture.BootstrapAsync(ct);

        var after =
            await ReadCorporateStateAsync(ct);

        Assert.Equal(before, after);

        await AssertReaderRoleRemainsRestrictedAsync(ct);
    }

    private async Task<CorporateState> ReadCorporateStateAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _fixture.OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT
                    (
                        SELECT count(*)
                        FROM pg_class AS c
                        JOIN pg_namespace AS n
                          ON n.oid = c.relnamespace
                        WHERE n.nspname = 'public'
                          AND c.relkind IN ('r', 'p')
                          AND c.relname IN (
                              'album',
                              'artist',
                              'customer',
                              'employee',
                              'genre',
                              'invoice',
                              'invoice_line',
                              'media_type',
                              'playlist',
                              'playlist_track',
                              'track'
                          )
                    ),
                    (
                        SELECT count(*)
                        FROM pdg.sales_summary
                    );
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(await reader.ReadAsync(ct));

        return new CorporateState(
            reader.GetInt64(0),
            reader.GetInt64(1));
    }

    private async Task AssertReaderRoleRemainsRestrictedAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _fixture.OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT
                    rolsuper,
                    rolcreatedb,
                    rolcreaterole,
                    rolbypassrls,
                    rolinherit,
                    (
                        SELECT count(*)
                        FROM pg_auth_members
                        WHERE member = r.oid
                    )
                FROM pg_roles AS r
                WHERE rolname = 'pdg_reader';
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(await reader.ReadAsync(ct));
        Assert.False(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
        Assert.False(reader.GetBoolean(4));
        Assert.Equal(0L, reader.GetInt64(5));
    }

    private sealed record CorporateState(
        long BaseTables,
        long SalesSummaryRows);
}

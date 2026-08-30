using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.IntegrationTests.Fixtures;
using Perimeter.Gateway.IntegrationTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T25;

[Collection(AcceptanceCollection.Name)]
public sealed class BootstrapIdempotenceTests
{
    private readonly AcceptanceEnvironment _environment;

    public BootstrapIdempotenceTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T25_Repeated_bootstrap_preserves_desired_state()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var before =
            await ReadEnvironmentStateAsync(ct);

        await _environment
            .PlatformStore
            .BootstrapAsync();

        await _environment
            .CorporateData
            .BootstrapAsync(ct);

        var after =
            await ReadEnvironmentStateAsync(ct);

        Assert.Equal(
            before,
            after);
    }

    [Fact]
    public async Task T25_Partial_platform_bootstrap_recovers_to_desired_state()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var fixture =
            new PlatformStoreIntegrationFixture();

        try
        {
            await fixture.Container.StartAsync(ct);

            await fixture.ApplyMigrationBundleAsync();

            await SqlScriptRunner.RunAsync(
                fixture.Container,
                PlatformStoreIntegrationFixture.DatabaseName,
                PlatformStoreIntegrationFixture.OwnerUsername,
                "/bootstrap/db/platform/10-platform-seed.sql",
                singleTransaction: true,
                cancellationToken: ct);

            Assert.False(
                await RoleExistsAsync(
                    fixture.OwnerConnectionString,
                    PlatformStoreIntegrationFixture.RuntimeUsername,
                    ct));

            Assert.Equal(
                3L,
                await CountAsync(
                    fixture.OwnerConnectionString,
                    """
                    SELECT count(*)
                    FROM pdg.subject;
                    """,
                    ct));

            await fixture.BootstrapAsync();

            Assert.True(
                await RoleExistsAsync(
                    fixture.OwnerConnectionString,
                    PlatformStoreIntegrationFixture.RuntimeUsername,
                    ct));

            Assert.Equal(
                new PlatformState(
                    Subjects: 3,
                    Actors: 1,
                    ActorCapabilities: 1,
                    Delegations: 3,
                    Resources: 1,
                    ResourceParameters: 1,
                    ResourceOutputFields: 4,
                    SubjectResourcePermissions: 3,
                    SubjectRowScopes: 17,
                    Migrations: 1),
                await ReadPlatformStateAsync(
                    fixture.OwnerConnectionString,
                    ct));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private async Task<EnvironmentState> ReadEnvironmentStateAsync(
        CancellationToken ct)
    {
        var platform =
            await ReadPlatformStateAsync(
                _environment
                    .PlatformStore
                    .OwnerConnectionString,
                ct);

        var corporate =
            await ReadCorporateStateAsync(ct);

        return new EnvironmentState(
            platform,
            corporate);
    }

    private static async Task<PlatformState> ReadPlatformStateAsync(
        string connectionString,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                connectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT
                    (SELECT count(*) FROM pdg.subject),
                    (SELECT count(*) FROM pdg.actor),
                    (SELECT count(*) FROM pdg.actor_capability),
                    (SELECT count(*) FROM pdg.delegation),
                    (SELECT count(*) FROM pdg.resource),
                    (SELECT count(*) FROM pdg.resource_parameter),
                    (SELECT count(*) FROM pdg.resource_output_field),
                    (SELECT count(*) FROM pdg.subject_resource_permission),
                    (SELECT count(*) FROM pdg.subject_row_scope),
                    (SELECT count(*) FROM pdg."__EFMigrationsHistory");
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new PlatformState(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5),
            reader.GetInt64(6),
            reader.GetInt64(7),
            reader.GetInt64(8),
            reader.GetInt64(9));
    }

    private async Task<CorporateState> ReadCorporateStateAsync(
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
                    (SELECT count(*) FROM pdg.sales_summary);
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new CorporateState(
            reader.GetInt64(0),
            reader.GetInt64(1));
    }

    private static async Task<bool> RoleExistsAsync(
        string connectionString,
        string roleName,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                connectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_roles
                    WHERE rolname = @role_name
                );
                """,
                connection);

        command.Parameters.AddWithValue(
            "role_name",
            roleName);

        return Convert.ToBoolean(
            await command.ExecuteScalarAsync(ct));
    }

    private static async Task<long> CountAsync(
        string connectionString,
        string sql,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                connectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                sql,
                connection);

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct));
    }

    private sealed record PlatformState(
        long Subjects,
        long Actors,
        long ActorCapabilities,
        long Delegations,
        long Resources,
        long ResourceParameters,
        long ResourceOutputFields,
        long SubjectResourcePermissions,
        long SubjectRowScopes,
        long Migrations);

    private sealed record CorporateState(
        long BaseTables,
        long SalesSummaryRows);

    private sealed record EnvironmentState(
        PlatformState Platform,
        CorporateState Corporate);
}
using Npgsql;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.Bootstrap;

public sealed class PlatformBootstrapIdempotenceTests
    : IClassFixture<PlatformStoreIntegrationFixture>
{
    private readonly PlatformStoreIntegrationFixture _fixture;

    public PlatformBootstrapIdempotenceTests(
        PlatformStoreIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repeated_platform_bootstrap_preserves_desired_state()
    {
        var ct = TestContext.Current.CancellationToken;

        var before =
            await ReadPlatformStateAsync(ct);

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
            before);

        await _fixture.BootstrapAsync();

        var after =
            await ReadPlatformStateAsync(ct);

        Assert.Equal(before, after);

        await AssertRuntimeRoleRemainsRestrictedAsync(ct);
    }

    private async Task<PlatformState> ReadPlatformStateAsync(
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

        Assert.True(await reader.ReadAsync(ct));

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

    private async Task AssertRuntimeRoleRemainsRestrictedAsync(
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
                WHERE rolname = 'pdg_platform_app';
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
}

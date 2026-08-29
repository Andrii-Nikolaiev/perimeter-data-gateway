using Npgsql;
using Perimeter.Gateway.IntegrationTests.Fixtures;
using Perimeter.Gateway.IntegrationTests.Helpers;

namespace Perimeter.Gateway.IntegrationTests.Bootstrap;

public sealed class PartialBootstrapRecoveryTests
{
    [Fact]
    public async Task Platform_bootstrap_recovers_after_interruption_before_runtime_grants()
    {
        var ct = TestContext.Current.CancellationToken;

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
                1L,
                await CountAsync(
                    fixture.OwnerConnectionString,
                    """
                    SELECT count(*)
                    FROM pdg."__EFMigrationsHistory";
                    """,
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
                1L,
                await CountAsync(
                    fixture.OwnerConnectionString,
                    """
                    SELECT count(*)
                    FROM pdg."__EFMigrationsHistory";
                    """,
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

            await using var runtimeConnection =
                new NpgsqlConnection(
                    fixture.RuntimeConnectionString);

            await runtimeConnection.OpenAsync(ct);

            await using var runtimeCommand =
                new NpgsqlCommand(
                    """
                    SELECT role_code
                    FROM pdg.subject
                    WHERE subject_id = 'user_42';
                    """,
                    runtimeConnection);

            Assert.Equal(
                "SalesManagerEurope",
                await runtimeCommand.ExecuteScalarAsync(ct));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static async Task<bool> RoleExistsAsync(
        string connectionString,
        string roleName,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

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
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                sql,
                connection);

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct));
    }
}

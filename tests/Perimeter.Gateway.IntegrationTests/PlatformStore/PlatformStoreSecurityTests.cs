using Npgsql;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.PlatformStore;

public sealed class PlatformStoreSecurityTests
    : IClassFixture<PlatformStoreIntegrationFixture>
{
    private readonly PlatformStoreIntegrationFixture _fixture;

    public PlatformStoreSecurityTests(
        PlatformStoreIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Runtime_role_has_only_the_required_platform_privileges()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var connection =
            new NpgsqlConnection(
                _fixture.RuntimeConnectionString);

        await connection.OpenAsync(ct);

        await using (var selectCommand =
            new NpgsqlCommand(
                """
                SELECT role_code
                FROM pdg.subject
                WHERE subject_id = 'user_42';
                """,
                connection))
        {
            var roleCode =
                Convert.ToString(
                    await selectCommand.ExecuteScalarAsync(ct));

            Assert.Equal(
                "SalesManagerEurope",
                roleCode);
        }

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            INSERT INTO pdg.subject (subject_id, role_code)
            VALUES ('forbidden_user', 'Forbidden');
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            UPDATE pdg.subject
            SET role_code = 'Forbidden'
            WHERE subject_id = 'user_42';
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            DELETE FROM pdg.subject
            WHERE subject_id = 'user_42';
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            SELECT *
            FROM pdg."__EFMigrationsHistory";
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            CREATE TABLE pdg.forbidden_runtime_table (
                id integer PRIMARY KEY
            );
            """,
            ct);
    }

    private static async Task AssertInsufficientPrivilegeAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken ct)
    {
        var exception =
            await Assert.ThrowsAsync<PostgresException>(
                async () =>
                {
                    await using var command =
                        new NpgsqlCommand(
                            sql,
                            connection);

                    await command.ExecuteNonQueryAsync(ct);
                });

        Assert.Equal(
            PostgresErrorCodes.InsufficientPrivilege,
            exception.SqlState);
    }
}

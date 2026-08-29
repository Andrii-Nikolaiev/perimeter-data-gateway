using Npgsql;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.CorporateData;

public sealed class CorporateDataSecurityTests
    : IClassFixture<CorporateDataIntegrationFixture>
{
    private readonly CorporateDataIntegrationFixture _fixture;

    public CorporateDataSecurityTests(
        CorporateDataIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Reader_role_can_only_read_the_published_projection()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var connection =
            new NpgsqlConnection(
                _fixture.RuntimeConnectionString);

        await connection.OpenAsync(ct);

        await using (var allowedCommand =
            new NpgsqlCommand(
                """
                SELECT count(*)
                FROM pdg.sales_summary;
                """,
                connection))
        {
            var count =
                Convert.ToInt32(
                    await allowedCommand.ExecuteScalarAsync(ct));

            Assert.True(count > 0);
        }

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            SELECT *
            FROM public.customer
            LIMIT 1;
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            INSERT INTO public.customer (
                customer_id,
                first_name,
                last_name
            )
            VALUES (
                999999,
                'Forbidden',
                'Insert'
            );
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            UPDATE public.customer
            SET first_name = 'Forbidden'
            WHERE customer_id = 1;
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            DELETE FROM public.customer
            WHERE customer_id = 1;
            """,
            ct);

        await AssertInsufficientPrivilegeAsync(
            connection,
            """
            CREATE TABLE pdg.forbidden_reader_table (
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

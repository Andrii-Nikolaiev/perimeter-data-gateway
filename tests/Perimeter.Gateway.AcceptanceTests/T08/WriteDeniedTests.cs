using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.T08;

[Collection(AcceptanceCollection.Name)]
public sealed class WriteDeniedTests
{
    private readonly AcceptanceEnvironment _environment;

    public WriteDeniedTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T08_Insert_update_delete_and_ddl_are_denied_for_pdg_reader()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var commands =
            new[]
            {
                """
                INSERT INTO public.customer
                DEFAULT VALUES;
                """,
                """
                UPDATE public.customer
                SET customer_id = customer_id
                WHERE FALSE;
                """,
                """
                DELETE FROM public.customer
                WHERE FALSE;
                """,
                """
                CREATE TABLE public.pdg_acceptance_write_probe
                (
                    id integer
                );
                """
            };

        foreach (var commandText in commands)
        {
            await AssertInsufficientPrivilegeAsync(
                commandText,
                ct);
        }
    }

    private async Task AssertInsufficientPrivilegeAsync(
        string commandText,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _environment
                    .CorporateData
                    .RuntimeConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                commandText,
                connection);

        var exception =
            await Assert.ThrowsAsync<PostgresException>(
                async () =>
                {
                    await command.ExecuteNonQueryAsync(ct);
                });

        Assert.Equal(
            "42501",
            exception.SqlState);
    }
}
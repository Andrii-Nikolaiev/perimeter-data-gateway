using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.T07;

[Collection(AcceptanceCollection.Name)]
public sealed class BaseTableSelectDeniedTests
{
    private readonly AcceptanceEnvironment _environment;

    public BaseTableSelectDeniedTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T07_Direct_select_from_base_table_is_denied_for_pdg_reader()
    {
        var ct =
            TestContext.Current.CancellationToken;

        await using var connection =
            new NpgsqlConnection(
                _environment
                    .CorporateData
                    .RuntimeConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT customer_id
                FROM public.customer
                LIMIT 1;
                """,
                connection);

        var exception =
            await Assert.ThrowsAsync<PostgresException>(
                async () =>
                {
                    await command.ExecuteScalarAsync(ct);
                });

        Assert.Equal(
            "42501",
            exception.SqlState);
    }
}
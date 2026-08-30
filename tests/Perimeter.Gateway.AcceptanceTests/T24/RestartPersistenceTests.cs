using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T24;

[Collection(AcceptanceCollection.Name)]
public sealed class RestartPersistenceTests
{
    private readonly AcceptanceEnvironment _environment;

    public RestartPersistenceTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T24_Restart_without_container_deletion_preserves_required_state()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var before =
            await ReadSnapshotAsync(ct);

        await _environment
            .CorporateData
            .Container
            .StopAsync(ct);

        await _environment
            .PlatformStore
            .Container
            .StopAsync(ct);

        try
        {
            await _environment
                .PlatformStore
                .Container
                .StartAsync(ct);

            await _environment
                .CorporateData
                .Container
                .StartAsync(ct);

            DatabaseConnectionPoolReset.ClearAll();

            var after =
                await ReadSnapshotAsync(ct);

            Assert.Equal(
                before,
                after);
        }
        finally
        {
            if (_environment.PlatformStore.Container.State
                != DotNet.Testcontainers.Containers.TestcontainersStates.Running)
            {
                await _environment
                    .PlatformStore
                    .Container
                    .StartAsync(ct);
            }

            if (_environment.CorporateData.Container.State
                != DotNet.Testcontainers.Containers.TestcontainersStates.Running)
            {
                await _environment
                    .CorporateData
                    .Container
                    .StartAsync(ct);
            }

            DatabaseConnectionPoolReset.ClearAll();
            _environment.RecreateWebHost();
        }
    }

    private async Task<PersistenceSnapshot> ReadSnapshotAsync(
        CancellationToken ct)
    {
        var platform =
            await ReadPlatformSnapshotAsync(ct);

        var corporate =
            await ReadCorporateSnapshotAsync(ct);

        return new PersistenceSnapshot(
            platform.Subjects,
            platform.Actors,
            platform.Delegations,
            platform.Resources,
            platform.Permissions,
            platform.RowScopes,
            corporate.Customers,
            corporate.Invoices,
            corporate.SalesSummaryRows);
    }

    private async Task<PlatformSnapshot> ReadPlatformSnapshotAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _environment
                    .PlatformStore
                    .OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
                SELECT
                    (SELECT count(*) FROM pdg.subject),
                    (SELECT count(*) FROM pdg.actor),
                    (SELECT count(*) FROM pdg.delegation),
                    (SELECT count(*) FROM pdg.resource),
                    (SELECT count(*) FROM pdg.subject_resource_permission),
                    (SELECT count(*) FROM pdg.subject_row_scope);
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new PlatformSnapshot(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    private async Task<CorporateSnapshot> ReadCorporateSnapshotAsync(
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
                    (SELECT count(*) FROM customer),
                    (SELECT count(*) FROM invoice),
                    (SELECT count(*) FROM pdg.sales_summary);
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new CorporateSnapshot(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2));
    }

    private sealed record PlatformSnapshot(
        long Subjects,
        long Actors,
        long Delegations,
        long Resources,
        long Permissions,
        long RowScopes);

    private sealed record CorporateSnapshot(
        long Customers,
        long Invoices,
        long SalesSummaryRows);

    private sealed record PersistenceSnapshot(
        long Subjects,
        long Actors,
        long Delegations,
        long Resources,
        long Permissions,
        long RowScopes,
        long Customers,
        long Invoices,
        long SalesSummaryRows);
}
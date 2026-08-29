using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perimeter.Gateway.Domain.Models;
using Perimeter.Gateway.Infrastructure.PlatformStore;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.PlatformStore;

public sealed class AuditWriterTests
    : IClassFixture<PlatformStoreIntegrationFixture>
{
    private readonly PlatformStoreIntegrationFixture _fixture;

    public AuditWriterTests(
        PlatformStoreIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Runtime_role_can_insert_audit_but_cannot_update_or_delete()
    {
        var ct = TestContext.Current.CancellationToken;
        var options =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(_fixture.RuntimeConnectionString)
                .Options;

        await using var dbContext =
            new PlatformStoreDbContext(options);

        var writer =
            new AuditWriter(dbContext);

        var record =
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "user_42",
                "sales_copilot_v1",
                "sales.read",
                "SalesSummary",
                "country=France",
                "ALLOW",
                "authorized",
                new Dictionary<string, string?>
                {
                    ["country"] = "France"
                },
                null,
                1);

        await writer.WriteAsync(
            record,
            ct);

        await using var connection =
            new NpgsqlConnection(
                _fixture.RuntimeConnectionString);

        await connection.OpenAsync(ct);

        await using (var selectCommand =
            new NpgsqlCommand(
                """
                SELECT count(*)
                FROM pdg.audit_record
                WHERE subject_id = 'user_42'
                  AND actor_id = 'sales_copilot_v1'
                  AND resource_name = 'SalesSummary'
                  AND decision = 'ALLOW';
                """,
                connection))
        {
            var count =
                Convert.ToInt32(
                    await selectCommand.ExecuteScalarAsync(ct));

            Assert.Equal(1, count);
        }

        var updateException =
            await Assert.ThrowsAsync<PostgresException>(
                async () =>
                {
                    await using var updateCommand =
                        new NpgsqlCommand(
                            """
                            UPDATE pdg.audit_record
                            SET decision = 'DENY'
                            WHERE subject_id = 'user_42';
                            """,
                            connection);

                    await updateCommand.ExecuteNonQueryAsync(ct);
                });

        Assert.Equal(
            PostgresErrorCodes.InsufficientPrivilege,
            updateException.SqlState);

        var deleteException =
            await Assert.ThrowsAsync<PostgresException>(
                async () =>
                {
                    await using var deleteCommand =
                        new NpgsqlCommand(
                            """
                            DELETE FROM pdg.audit_record
                            WHERE subject_id = 'user_42';
                            """,
                            connection);

                    await deleteCommand.ExecuteNonQueryAsync(ct);
                });

        Assert.Equal(
            PostgresErrorCodes.InsufficientPrivilege,
            deleteException.SqlState);
    }
}

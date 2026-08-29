using Npgsql;
using Perimeter.Gateway.IntegrationTests.Helpers;
using Testcontainers.PostgreSql;

namespace Perimeter.Gateway.IntegrationTests.Fixtures;

public sealed class CorporateDataIntegrationFixture : IAsyncLifetime
{
    public const string DatabaseName = "chinook";
    public const string OwnerUsername = "chinook_owner";
    public const string RuntimeUsername = "pdg_reader";

    public CorporateDataIntegrationFixture()
    {
        OwnerPassword = $"owner_{Guid.NewGuid():N}";
        RuntimePassword = $"reader_{Guid.NewGuid():N}";

        Container = PostgreSqlContainerFactory.Create(
            DatabaseName,
            OwnerUsername,
            OwnerPassword);
    }

    public PostgreSqlContainer Container { get; }

    public string OwnerPassword { get; }

    public string RuntimePassword { get; }

    public string OwnerConnectionString =>
        Container.GetConnectionString();

    public string RuntimeConnectionString
    {
        get
        {
            var builder =
                new NpgsqlConnectionStringBuilder(
                    OwnerConnectionString)
                {
                    Username = RuntimeUsername,
                    Password = RuntimePassword
                };

            return builder.ConnectionString;
        }
    }

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await Container.StartAsync(ct);

        await BootstrapAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public async Task BootstrapAsync(
        CancellationToken ct)
    {
        await VerifyPinnedArtifactAsync(ct);
        await EnsureChinookDatasetAsync(ct);

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/chinook/20-create-pdg-schema-view.sql",
            cancellationToken: ct);

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/chinook/30-create-pdg-reader.sql",
            new Dictionary<string, string>
            {
                ["pdg_reader_password"] = RuntimePassword
            },
            cancellationToken: ct);

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/chinook/40-corporate-grants.sql",
            cancellationToken: ct);

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/chinook/50-verify-corporate-security.sql",
            cancellationToken: ct);
    }

    private async Task VerifyPinnedArtifactAsync(
        CancellationToken ct)
    {
        var result =
            await Container.ExecAsync(
                new[]
                {
                    "/bin/sh",
                    "-c",
                    "cd /bootstrap && sha256sum -c db/chinook/10-chinook-1.4.5.sha256"
                },
                ct);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Pinned Chinook checksum verification failed. " +
                $"Exit code: {result.ExitCode}. " +
                $"stderr: {result.Stderr}");
        }
    }

    private async Task EnsureChinookDatasetAsync(
        CancellationToken ct)
    {
        var tableCount =
            await GetChinookTableCountAsync(ct);

        switch (tableCount)
        {
            case 0:
                await ImportChinookAsync(ct);
                break;

            case 11:
                break;

            default:
                throw new InvalidOperationException(
                    "Partial Chinook dataset state detected: " +
                    $"{tableCount} of 11 expected tables.");
        }
    }

    private async Task<int> GetChinookTableCountAsync(
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                """
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
                  );
                """,
                connection);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(ct));
    }

    private async Task ImportChinookAsync(
        CancellationToken ct)
    {
        var command =
            """
            awk '
                $0 != "DROP DATABASE IF EXISTS chinook;" &&
                $0 != "CREATE DATABASE chinook;" &&
                $0 != "\\c chinook;"
            ' /bootstrap/db/chinook/10-chinook-1.4.5.sql \
                > /tmp/chinook-import.sql &&
            psql \
                -U chinook_owner \
                -d chinook \
                -v ON_ERROR_STOP=1 \
                --single-transaction \
                -f /tmp/chinook-import.sql &&
            rm -f /tmp/chinook-import.sql
            """;

        var result =
            await Container.ExecAsync(
                new[]
                {
                    "/bin/sh",
                    "-c",
                    command
                },
                ct);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Chinook import failed. " +
                $"Exit code: {result.ExitCode}. " +
                $"stderr: {result.Stderr}");
        }
    }
}

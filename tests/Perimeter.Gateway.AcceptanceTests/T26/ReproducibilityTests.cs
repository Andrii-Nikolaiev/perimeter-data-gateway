using System.Net;
using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.T26;

[Collection(AcceptanceCollection.Name)]
public sealed class ReproducibilityTests
{
    private readonly AcceptanceEnvironment _environment;

    public ReproducibilityTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T26_Clean_documented_startup_produces_expected_demo_environment()
    {
        var ct =
            TestContext.Current.CancellationToken;

        await AssertDocumentedCleanStartupAsync(ct);

        var platform =
            await ReadPlatformStateAsync(ct);

        Assert.Equal(
            new PlatformState(
                Subjects: 3,
                Actors: 1,
                Delegations: 3,
                Resources: 1,
                Permissions: 3,
                RowScopes: 17),
            platform);

        var corporate =
            await ReadCorporateStateAsync(ct);

        Assert.Equal(
            new CorporateState(
                Customers: 59,
                Invoices: 412,
                SalesSummaryRows: 412),
            corporate);

        using var response =
            await _environment.Client.GetAsync(
                "/health/ready",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private static async Task AssertDocumentedCleanStartupAsync(
        CancellationToken ct)
    {
        var repositoryRoot =
            FindRepositoryRoot();

        var readmePath =
            Path.Combine(
                repositoryRoot,
                "docs",
                "README.md");

        Assert.True(
            File.Exists(readmePath));

        var readme =
            await File.ReadAllTextAsync(
                readmePath,
                ct);

        Assert.Contains(
            "docker compose down -v --remove-orphans",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "docker compose up --build -d",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "docker compose ps -a",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "curl -f http://127.0.0.1:8080/health/ready",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "acceptance test **T-26**",
            readme,
            StringComparison.Ordinal);
    }

    private async Task<PlatformState> ReadPlatformStateAsync(
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

        return new PlatformState(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
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
                    (SELECT count(*) FROM customer),
                    (SELECT count(*) FROM invoice),
                    (SELECT count(*) FROM pdg.sales_summary);
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct));

        return new CorporateState(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2));
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                Path.Combine(
                    directory.FullName,
                    "Perimeter.Gateway.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Repository root containing Perimeter.Gateway.sln was not found.");
    }

    private sealed record PlatformState(
        long Subjects,
        long Actors,
        long Delegations,
        long Resources,
        long Permissions,
        long RowScopes);

    private sealed record CorporateState(
        long Customers,
        long Invoices,
        long SalesSummaryRows);
}
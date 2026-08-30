using System.Diagnostics;
using Npgsql;
using Perimeter.Gateway.IntegrationTests.Helpers;
using Testcontainers.PostgreSql;

namespace Perimeter.Gateway.IntegrationTests.Fixtures;

public sealed class PlatformStoreIntegrationFixture : IAsyncLifetime
{
    public const string DatabaseName = "pdg_platform_store";
    public const string OwnerUsername = "pdg_platform_owner";
    public const string RuntimeUsername = "pdg_platform_app";

    private static readonly SemaphoreSlim MigrationBundleLock = new(1, 1);
    private static string? migrationBundlePath;

    public PlatformStoreIntegrationFixture()
    {
        OwnerPassword = $"owner_{Guid.NewGuid():N}";
        RuntimePassword = $"app_{Guid.NewGuid():N}";

        Container = PostgreSqlContainerFactory.Create(
            DatabaseName,
            OwnerUsername,
            OwnerPassword,
            new Dictionary<string, string>
            {
                ["PLATFORM_APP_PASSWORD"] = RuntimePassword
            });
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
        await Container.StartAsync();
        await BootstrapAsync();
    }

    public async Task BootstrapAsync()
    {
        await ApplyMigrationBundleAsync();

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/platform/10-platform-seed.sql",
            singleTransaction: true);

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/platform/20-create-platform-runtime-role.sql",
            new Dictionary<string, string>
            {
                ["platform_app_password"] = "PLATFORM_APP_PASSWORD"
            });

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/platform/30-platform-grants.sql");

        await SqlScriptRunner.RunAsync(
            Container,
            DatabaseName,
            OwnerUsername,
            "/bootstrap/db/platform/40-verify-platform-security.sql");
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public async Task ApplyMigrationBundleAsync()
    {
        var bundlePath =
            await GetOrBuildMigrationBundleAsync();

        var startInfo =
            new ProcessStartInfo
            {
                FileName = bundlePath,
                WorkingDirectory = FindRepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add("--connection");
        startInfo.ArgumentList.Add(OwnerConnectionString);

        using var process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Failed to start Platform Store migration bundle.");

        var standardOutput =
            process.StandardOutput.ReadToEndAsync();

        var standardError =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await standardOutput;
        var stderr = await standardError;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Platform Store migration bundle failed. " +
                $"Exit code: {process.ExitCode}. " +
                $"stdout: {stdout} " +
                $"stderr: {stderr}");
        }
    }

    private static async Task<string> GetOrBuildMigrationBundleAsync()
    {
        await MigrationBundleLock.WaitAsync();

        try
        {
            if (migrationBundlePath is not null &&
                File.Exists(migrationBundlePath))
            {
                return migrationBundlePath;
            }

            var repositoryRoot = FindRepositoryRoot();

            var bundleDirectory = Path.Combine(
                Path.GetTempPath(),
                "pdg-integration-tests",
                Environment.ProcessId.ToString());

            Directory.CreateDirectory(bundleDirectory);

            var bundleFileName =
                OperatingSystem.IsWindows()
                    ? "pdg-platform-migrate.exe"
                    : "pdg-platform-migrate";

            var bundlePath = Path.Combine(
                bundleDirectory,
                bundleFileName);

            await RunDotNetAsync(
                repositoryRoot,
                "tool",
                "restore");

            await RunDotNetAsync(
                repositoryRoot,
                "ef",
                "migrations",
                "bundle",
                "--project",
                "src/Perimeter.Gateway.Infrastructure",
                "--startup-project",
                "src/Perimeter.Gateway.Api",
                "--context",
                "PlatformStoreDbContext",
                "--configuration",
                "Release",
                "--output",
                bundlePath,
                "--force");

            migrationBundlePath = bundlePath;

            return migrationBundlePath;
        }
        finally
        {
            MigrationBundleLock.Release();
        }
    }

    private static async Task RunDotNetAsync(
        string workingDirectory,
        params string[] arguments)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Failed to start dotnet process.");

        var standardOutput =
            process.StandardOutput.ReadToEndAsync();

        var standardError =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await standardOutput;
        var stderr = await standardError;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "dotnet command failed. " +
                $"Exit code: {process.ExitCode}. " +
                $"stdout: {stdout} " +
                $"stderr: {stderr}");
        }
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
}

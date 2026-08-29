using Testcontainers.PostgreSql;

namespace Perimeter.Gateway.IntegrationTests.Fixtures;

public static class PostgreSqlContainerFactory
{
    private const string PostgreSqlImage = "postgres:18.6-alpine";

    public static PostgreSqlContainer Create(
        string database,
        string username,
        string password)
    {
        var repositoryRoot = FindRepositoryRoot();
        var databaseScriptsPath = Path.Combine(
            repositoryRoot,
            "db");

        return new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase(database)
            .WithUsername(username)
            .WithPassword(password)
            .WithResourceMapping(
                new DirectoryInfo(databaseScriptsPath),
                "/bootstrap/db")
            .Build();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(
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

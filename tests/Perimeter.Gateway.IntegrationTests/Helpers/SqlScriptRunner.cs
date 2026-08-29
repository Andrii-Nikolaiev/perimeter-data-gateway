using Testcontainers.PostgreSql;

namespace Perimeter.Gateway.IntegrationTests.Helpers;

public static class SqlScriptRunner
{
    public static async Task RunAsync(
        PostgreSqlContainer container,
        string database,
        string username,
        string containerScriptPath,
        IReadOnlyDictionary<string, string>? variables = null,
        bool singleTransaction = false,
        CancellationToken cancellationToken = default)
    {
        var command = new List<string>
        {
            "psql",
            "-U",
            username,
            "-d",
            database,
            "-v",
            "ON_ERROR_STOP=1"
        };

        if (singleTransaction)
        {
            command.Add("--single-transaction");
        }

        if (variables is not null)
        {
            foreach (var variable in variables)
            {
                command.Add("-v");
                command.Add($"{variable.Key}={variable.Value}");
            }
        }

        command.Add("-f");
        command.Add(containerScriptPath);

        var result = await container.ExecAsync(
            command,
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"psql failed for {containerScriptPath}. " +
                $"Exit code: {result.ExitCode}. " +
                $"stderr: {result.Stderr}");
        }
    }
}

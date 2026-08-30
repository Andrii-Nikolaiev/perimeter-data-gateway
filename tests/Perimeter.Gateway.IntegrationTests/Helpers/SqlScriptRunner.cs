using Testcontainers.PostgreSql;

namespace Perimeter.Gateway.IntegrationTests.Helpers;

public static class SqlScriptRunner
{
    public static async Task RunAsync(
        PostgreSqlContainer container,
        string database,
        string username,
        string containerScriptPath,
        IReadOnlyDictionary<string, string>? variableEnvironmentNames = null,
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

        if (variableEnvironmentNames is not null)
        {
            foreach (var variable in variableEnvironmentNames)
            {
                ValidateEnvironmentVariableName(
                    variable.Value);

                command.Add("-v");
                command.Add(
                    $"{variable.Key}=\"${variable.Value}\"");
            }
        }

        command.Add("-f");
        command.Add(containerScriptPath);

        var shellCommand =
            string.Join(
                " ",
                command);

        var result =
            await container.ExecAsync(
                new[]
                {
                    "/bin/sh",
                    "-c",
                    shellCommand
                },
                cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"psql failed for {containerScriptPath}. " +
                $"Exit code: {result.ExitCode}. " +
                $"stderr: {result.Stderr}");
        }
    }

    private static void ValidateEnvironmentVariableName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            !(char.IsLetter(name[0]) || name[0] == '_') ||
            name.Any(
                character =>
                    !(char.IsLetterOrDigit(character) ||
                      character == '_')))
        {
            throw new ArgumentException(
                "Invalid environment variable name.",
                nameof(name));
        }
    }
}

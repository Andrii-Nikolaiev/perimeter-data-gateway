using Npgsql;

namespace Perimeter.Gateway.Infrastructure.CorporateData;

public sealed class CorporateDataProbe
{
    private readonly string _connectionString;

    public CorporateDataProbe(
        string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> IsReachableAsync(
        CancellationToken ct)
    {
        try
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(ct);

            return true;
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }
}

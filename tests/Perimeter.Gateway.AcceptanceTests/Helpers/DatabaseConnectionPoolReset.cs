using Npgsql;

namespace Perimeter.Gateway.AcceptanceTests.Helpers;

public static class DatabaseConnectionPoolReset
{
    public static void ClearAll()
    {
        NpgsqlConnection.ClearAllPools();
    }
}
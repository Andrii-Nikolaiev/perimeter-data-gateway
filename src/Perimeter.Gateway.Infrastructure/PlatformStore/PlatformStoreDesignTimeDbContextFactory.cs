using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

public sealed class PlatformStoreDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<PlatformStoreDbContext>
{
    public PlatformStoreDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "PDG_DESIGNTIME_PLATFORM_CONNECTION")
            ?? "Host=localhost;Database=pdg_platform_store;Username=pdg_platform_owner";

        var options =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        "pdg"))
                .Options;

        return new PlatformStoreDbContext(options);
    }
}
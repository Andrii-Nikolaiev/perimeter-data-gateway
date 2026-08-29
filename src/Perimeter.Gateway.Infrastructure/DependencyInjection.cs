using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Infrastructure.CorporateData;
using Perimeter.Gateway.Infrastructure.PlatformStore;
using Perimeter.Gateway.Infrastructure.Time;

namespace Perimeter.Gateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPdgInfrastructure(
        this IServiceCollection services,
        string platformStoreConnectionString,
        string corporateDataSourceConnectionString)
    {
        if (string.IsNullOrWhiteSpace(platformStoreConnectionString))
        {
            throw new InvalidOperationException(
                "Platform Store connection string is required.");
        }

        if (string.IsNullOrWhiteSpace(corporateDataSourceConnectionString))
        {
            throw new InvalidOperationException(
                "Corporate Data Source connection string is required.");
        }

        services.AddDbContext<PlatformStoreDbContext>(
            options =>
                options.UseNpgsql(
                    platformStoreConnectionString,
                    npgsql =>
                        npgsql.MigrationsHistoryTable(
                            "__EFMigrationsHistory",
                            "pdg")));

        services.AddScoped<IPlatformStore, PlatformStoreRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<PlatformStoreProbe>();

        services.AddScoped<ICorporateDataReader>(
            _ => new CorporateDataReader(
                corporateDataSourceConnectionString));

        services.AddScoped<CorporateDataProbe>(
            _ => new CorporateDataProbe(
                corporateDataSourceConnectionString));

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}

using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Audit;
using Perimeter.Gateway.Application.Authorization;
using Perimeter.Gateway.Application.Resources;
using Perimeter.Gateway.Infrastructure;

namespace Perimeter.Gateway.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPdgServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var platformStoreConnectionString =
            configuration.GetConnectionString("PlatformStore");

        if (string.IsNullOrWhiteSpace(platformStoreConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:PlatformStore is required.");
        }

        var corporateDataSourceConnectionString =
            configuration.GetConnectionString("CorporateDataSource");

        if (string.IsNullOrWhiteSpace(corporateDataSourceConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:CorporateDataSource is required.");
        }

        services.AddPdgInfrastructure(
            platformStoreConnectionString,
            corporateDataSourceConnectionString);

        services.AddScoped<
            IAccessPolicyEvaluator,
            AuthorizationPolicyEvaluator>();

        services.AddScoped<SalesSummaryRequestValidator>();
        services.AddScoped<AuditRecordFactory>();
        services.AddScoped<GetSalesSummaryHandler>();

        return services;
    }
}
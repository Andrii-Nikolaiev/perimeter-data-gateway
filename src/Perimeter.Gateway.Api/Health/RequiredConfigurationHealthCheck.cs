using Microsoft.Extensions.Diagnostics.HealthChecks;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;

namespace Perimeter.Gateway.Api.Health;

public sealed class RequiredConfigurationHealthCheck
    : IHealthCheck
{
    private const string RequiredResourceName = "SalesSummary";

    private readonly IPlatformStore _platformStore;

    public RequiredConfigurationHealthCheck(
        IPlatformStore platformStore)
    {
        _platformStore = platformStore;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resource =
                await _platformStore.GetPublishedResourceAsync(
                    RequiredResourceName,
                    cancellationToken);

            if (resource is null ||
                resource.MaxRows <= 0 ||
                resource.MaxRows == int.MaxValue)
            {
                return HealthCheckResult.Unhealthy();
            }

            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PdgException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

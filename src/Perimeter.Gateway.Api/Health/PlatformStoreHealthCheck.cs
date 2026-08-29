using Microsoft.Extensions.Diagnostics.HealthChecks;
using Perimeter.Gateway.Infrastructure.PlatformStore;

namespace Perimeter.Gateway.Api.Health;

public sealed class PlatformStoreHealthCheck
    : IHealthCheck
{
    private readonly PlatformStoreProbe _probe;

    public PlatformStoreHealthCheck(
        PlatformStoreProbe probe)
    {
        _probe = probe;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var reachable =
            await _probe.IsReachableAsync(cancellationToken);

        return reachable
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }
}

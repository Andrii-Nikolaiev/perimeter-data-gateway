using Microsoft.Extensions.Diagnostics.HealthChecks;
using Perimeter.Gateway.Infrastructure.CorporateData;

namespace Perimeter.Gateway.Api.Health;

public sealed class CorporateDataHealthCheck
    : IHealthCheck
{
    private readonly CorporateDataProbe _probe;

    public CorporateDataHealthCheck(
        CorporateDataProbe probe)
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

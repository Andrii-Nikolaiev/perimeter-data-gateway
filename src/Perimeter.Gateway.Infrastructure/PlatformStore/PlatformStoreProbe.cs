using Microsoft.EntityFrameworkCore;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

public sealed class PlatformStoreProbe
{
    private readonly PlatformStoreDbContext _dbContext;

    public PlatformStoreProbe(
        PlatformStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsReachableAsync(
        CancellationToken ct)
    {
        return _dbContext.Database.CanConnectAsync(ct);
    }
}

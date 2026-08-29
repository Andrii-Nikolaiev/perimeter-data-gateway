using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perimeter.Gateway.Infrastructure.CorporateData;
using Perimeter.Gateway.Infrastructure.PlatformStore;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.Health;

public sealed class DependencyProbeTests
    : IClassFixture<PlatformStoreIntegrationFixture>,
      IClassFixture<CorporateDataIntegrationFixture>
{
    private readonly PlatformStoreIntegrationFixture _platformFixture;
    private readonly CorporateDataIntegrationFixture _corporateFixture;

    public DependencyProbeTests(
        PlatformStoreIntegrationFixture platformFixture,
        CorporateDataIntegrationFixture corporateFixture)
    {
        _platformFixture = platformFixture;
        _corporateFixture = corporateFixture;
    }

    [Fact]
    public async Task Probes_report_reachable_and_unreachable_dependencies()
    {
        var ct = TestContext.Current.CancellationToken;

        var reachablePlatformOptions =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(
                    _platformFixture.RuntimeConnectionString)
                .Options;

        await using var reachablePlatformContext =
            new PlatformStoreDbContext(
                reachablePlatformOptions);

        var reachablePlatformProbe =
            new PlatformStoreProbe(
                reachablePlatformContext);

        var reachableCorporateProbe =
            new CorporateDataProbe(
                _corporateFixture.RuntimeConnectionString);

        Assert.True(
            await reachablePlatformProbe.IsReachableAsync(ct));

        Assert.True(
            await reachableCorporateProbe.IsReachableAsync(ct));

        var unavailablePlatformConnectionString =
            CreateUnavailableConnectionString(
                _platformFixture.RuntimeConnectionString);

        var unavailableCorporateConnectionString =
            CreateUnavailableConnectionString(
                _corporateFixture.RuntimeConnectionString);

        var unavailablePlatformOptions =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(
                    unavailablePlatformConnectionString)
                .Options;

        await using var unavailablePlatformContext =
            new PlatformStoreDbContext(
                unavailablePlatformOptions);

        var unavailablePlatformProbe =
            new PlatformStoreProbe(
                unavailablePlatformContext);

        var unavailableCorporateProbe =
            new CorporateDataProbe(
                unavailableCorporateConnectionString);

        Assert.False(
            await unavailablePlatformProbe.IsReachableAsync(ct));

        Assert.False(
            await unavailableCorporateProbe.IsReachableAsync(ct));
    }

    private static string CreateUnavailableConnectionString(
        string connectionString)
    {
        var builder =
            new NpgsqlConnectionStringBuilder(
                connectionString)
            {
                Host = "127.0.0.1",
                Port = 1,
                Pooling = false,
                Timeout = 1
            };

        return builder.ConnectionString;
    }
}

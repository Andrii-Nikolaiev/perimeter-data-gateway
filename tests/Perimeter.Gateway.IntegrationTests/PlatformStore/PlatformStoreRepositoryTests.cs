using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Infrastructure.PlatformStore;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.IntegrationTests.PlatformStore;

public sealed class PlatformStoreRepositoryTests
    : IClassFixture<PlatformStoreIntegrationFixture>
{
    private readonly PlatformStoreIntegrationFixture _fixture;

    public PlatformStoreRepositoryTests(
        PlatformStoreIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repository_reads_seeded_platform_configuration()
    {
        var options =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(_fixture.RuntimeConnectionString)
                .Options;

        await using var dbContext =
            new PlatformStoreDbContext(options);

        var repository =
            new PlatformStoreRepository(dbContext);

        var subject = await repository.GetSubjectAsync(
            "user_42",
            CancellationToken.None);

        var actor = await repository.GetActorAsync(
            "sales_copilot_v1",
            CancellationToken.None);

        var delegation = await repository.GetDelegationAsync(
            "user_42",
            "sales_copilot_v1",
            CancellationToken.None);

        var capabilities =
            await repository.GetActorCapabilitiesAsync(
                "sales_copilot_v1",
                CancellationToken.None);

        var resource =
            await repository.GetPublishedResourceAsync(
                "SalesSummary",
                CancellationToken.None);

        var permission =
            await repository.GetSubjectResourcePermissionAsync(
                "user_42",
                "SalesSummary",
                CancellationToken.None);

        var rowScope =
            await repository.GetSubjectRowScopeValuesAsync(
                "user_42",
                "SalesSummary",
                "country",
                CancellationToken.None);

        Assert.NotNull(subject);
        Assert.NotNull(actor);
        Assert.NotNull(delegation);
        Assert.Contains("sales.read", capabilities);
        Assert.NotNull(resource);
        Assert.NotNull(permission);
        Assert.Contains("France", rowScope);
        Assert.DoesNotContain("USA", rowScope);
    }

    [Fact]
    public async Task Repository_maps_database_failure_to_platform_store_unavailable()
    {
        var ct = TestContext.Current.CancellationToken;

        var connectionString =
            new NpgsqlConnectionStringBuilder(
                _fixture.RuntimeConnectionString)
            {
                Host = "127.0.0.1",
                Port = 1,
                Pooling = false,
                Timeout = 1
            }
            .ConnectionString;

        var options =
            new DbContextOptionsBuilder<PlatformStoreDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        await using var dbContext =
            new PlatformStoreDbContext(options);

        var repository =
            new PlatformStoreRepository(dbContext);

        var exception =
            await Assert.ThrowsAsync<PdgException>(
                () => repository.GetSubjectAsync(
                    "user_42",
                    ct));

        Assert.Equal(
            PdgErrorCategory.PlatformStoreUnavailable,
            exception.Category);

        Assert.NotNull(exception.InnerException);
    }
}

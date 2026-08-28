using Microsoft.EntityFrameworkCore;
using Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

public sealed class PlatformStoreDbContext : DbContext
{
    public PlatformStoreDbContext(
        DbContextOptions<PlatformStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<SubjectEntity> Subjects => Set<SubjectEntity>();

    public DbSet<ActorEntity> Actors => Set<ActorEntity>();

    public DbSet<ActorCapabilityEntity> ActorCapabilities =>
        Set<ActorCapabilityEntity>();

    public DbSet<DelegationEntity> Delegations =>
        Set<DelegationEntity>();

    public DbSet<ResourceEntity> Resources =>
        Set<ResourceEntity>();

    public DbSet<ResourceParameterEntity> ResourceParameters =>
        Set<ResourceParameterEntity>();

    public DbSet<ResourceOutputFieldEntity> ResourceOutputFields =>
        Set<ResourceOutputFieldEntity>();

    public DbSet<SubjectResourcePermissionEntity> SubjectResourcePermissions =>
        Set<SubjectResourcePermissionEntity>();

    public DbSet<SubjectRowScopeEntity> SubjectRowScopes =>
        Set<SubjectRowScopeEntity>();

    public DbSet<AuditRecordEntity> AuditRecords =>
        Set<AuditRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("pdg");
	PlatformStoreMappings.Configure(modelBuilder);
    }
}
using Microsoft.EntityFrameworkCore;
using Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

internal static class PlatformStoreMappings
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureSubject(modelBuilder);
        ConfigureActor(modelBuilder);
        ConfigureActorCapability(modelBuilder);
        ConfigureDelegation(modelBuilder);
        ConfigureResource(modelBuilder);
        ConfigureResourceParameter(modelBuilder);
        ConfigureResourceOutputField(modelBuilder);
        ConfigureSubjectResourcePermission(modelBuilder);
        ConfigureSubjectRowScope(modelBuilder);
        ConfigureAuditRecord(modelBuilder);
    }

    private static void ConfigureSubject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectEntity>(entity =>
        {
            entity.ToTable("subject");

            entity.HasKey(x => x.SubjectId);

            entity.Property(x => x.SubjectId)
                .HasColumnName("subject_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.RoleCode)
                .HasColumnName("role_code")
                .HasColumnType("text")
                .IsRequired();
        });
    }

    private static void ConfigureActor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActorEntity>(entity =>
        {
            entity.ToTable("actor");

            entity.HasKey(x => x.ActorId);

            entity.Property(x => x.ActorId)
                .HasColumnName("actor_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ActorType)
                .HasColumnName("actor_type")
                .HasColumnType("text")
                .IsRequired();
        });
    }

    private static void ConfigureActorCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActorCapabilityEntity>(entity =>
        {
            entity.ToTable("actor_capability");

            entity.HasKey(x => new
            {
                x.ActorId,
                x.Capability
            });

            entity.Property(x => x.ActorId)
                .HasColumnName("actor_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Capability)
                .HasColumnName("capability")
                .HasColumnType("text")
                .IsRequired();

            entity.HasOne<ActorEntity>()
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureDelegation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DelegationEntity>(entity =>
        {
            entity.ToTable("delegation");

            entity.HasKey(x => new
            {
                x.SubjectId,
                x.ActorId
            });

            entity.Property(x => x.SubjectId)
                .HasColumnName("subject_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ActorId)
                .HasColumnName("actor_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasOne<SubjectEntity>()
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<ActorEntity>()
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureResource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceEntity>(entity =>
        {
            entity.ToTable(
                "resource",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_resource_max_rows",
                        "max_rows > 0");
                });

            entity.HasKey(x => x.ResourceName);

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.RequiredCapability)
                .HasColumnName("required_capability")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.MaxRows)
                .HasColumnName("max_rows")
                .IsRequired();
        });
    }

    private static void ConfigureResourceParameter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceParameterEntity>(entity =>
        {
            entity.ToTable("resource_parameter");

            entity.HasKey(x => new
            {
                x.ResourceName,
                x.ParamName
            });

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ParamName)
                .HasColumnName("param_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ParamType)
                .HasColumnName("param_type")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Required)
                .HasColumnName("required")
                .IsRequired();

            entity.HasOne<ResourceEntity>()
                .WithMany()
                .HasForeignKey(x => x.ResourceName)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureResourceOutputField(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceOutputFieldEntity>(entity =>
        {
            entity.ToTable(
                "resource_output_field",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_resource_output_field_ordinal",
                        "ordinal > 0");
                });

            entity.HasKey(x => new
            {
                x.ResourceName,
                x.FieldName
            });

            entity.HasAlternateKey(x => new
            {
                x.ResourceName,
                x.Ordinal
            });

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.FieldName)
                .HasColumnName("field_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Ordinal)
                .HasColumnName("ordinal")
                .IsRequired();

            entity.HasOne<ResourceEntity>()
                .WithMany()
                .HasForeignKey(x => x.ResourceName)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureSubjectResourcePermission(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectResourcePermissionEntity>(entity =>
        {
            entity.ToTable(
                "subject_resource_permission",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_subject_resource_permission_row_scope_mode",
                        "row_scope_mode IN ('ALL', 'ALLOW_LIST')");
                });

            entity.HasKey(x => new
            {
                x.SubjectId,
                x.ResourceName
            });

            entity.Property(x => x.SubjectId)
                .HasColumnName("subject_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Allowed)
                .HasColumnName("allowed")
                .IsRequired();

            entity.Property(x => x.RowScopeMode)
                .HasColumnName("row_scope_mode")
                .HasColumnType("text")
                .IsRequired();

            entity.HasOne<SubjectEntity>()
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<ResourceEntity>()
                .WithMany()
                .HasForeignKey(x => x.ResourceName)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureSubjectRowScope(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectRowScopeEntity>(entity =>
        {
            entity.ToTable("subject_row_scope");

            entity.HasKey(x => new
            {
                x.SubjectId,
                x.ResourceName,
                x.Dimension,
                x.AllowedValue
            });

            entity.Property(x => x.SubjectId)
                .HasColumnName("subject_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Dimension)
                .HasColumnName("dimension")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.AllowedValue)
                .HasColumnName("allowed_value")
                .HasColumnType("text")
                .IsRequired();

            entity.HasOne<SubjectResourcePermissionEntity>()
                .WithMany()
                .HasForeignKey(x => new
                {
                    x.SubjectId,
                    x.ResourceName
                })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAuditRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditRecordEntity>(entity =>
        {
            entity.ToTable(
                "audit_record",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_audit_record_decision",
                        "decision IN ('ALLOW', 'DENY')");

                    table.HasCheckConstraint(
                        "ck_audit_record_rows_returned",
                        "rows_returned >= 0");

                    table.HasCheckConstraint(
                        "ck_audit_record_deny_rows_returned",
                        "decision = 'ALLOW' OR rows_returned = 0");
                });

            entity.HasKey(x => x.AuditId);

            entity.Property(x => x.AuditId)
                .HasColumnName("audit_id")
                .UseIdentityAlwaysColumn();

            entity.Property(x => x.OccurredAt)
                .HasColumnName("occurred_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.SubjectId)
                .HasColumnName("subject_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ActorId)
                .HasColumnName("actor_id")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Capability)
                .HasColumnName("capability")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Scope)
                .HasColumnName("scope")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Decision)
                .HasColumnName("decision")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ReasonCategory)
                .HasColumnName("reason_category")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.NormalizedParameters)
                .HasColumnName("normalized_parameters")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            entity.Property(x => x.EffectiveRowScope)
                .HasColumnName("effective_row_scope")
                .HasColumnType("jsonb");

            entity.Property(x => x.RowsReturned)
                .HasColumnName("rows_returned")
                .IsRequired();

            entity.HasIndex(x => x.OccurredAt)
                .HasDatabaseName("ix_audit_record_occurred_at");

            entity.HasIndex(x => new
                {
                    x.SubjectId,
                    x.ActorId
                })
                .HasDatabaseName("ix_audit_record_subject_actor");
        });
    }
}
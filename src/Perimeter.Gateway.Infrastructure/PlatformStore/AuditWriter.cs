using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;
using Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

public sealed class AuditWriter : IAuditWriter
{
    private readonly PlatformStoreDbContext _dbContext;

    public AuditWriter(
        PlatformStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(
        AuditRecord record,
        CancellationToken ct)
    {
        AuditRecordEntity? entity = null;

        try
        {
            entity = new AuditRecordEntity
            {
                OccurredAt = record.Timestamp,
                SubjectId = record.Subject,
                ActorId = record.Actor,
                Capability = record.Capability,
                ResourceName = record.Resource,
                Scope = record.Scope,
                Decision = record.Decision,
                ReasonCategory = record.ReasonCategory,
                NormalizedParameters =
                    JsonSerializer.Serialize(
                        record.NormalizedParameters),
                EffectiveRowScope =
                    record.EffectiveRowScope is null
                        ? null
                        : JsonSerializer.Serialize(
                            record.EffectiveRowScope),
                RowsReturned = record.RowsReturned
            };

            _dbContext.AuditRecords.Add(entity);

            await _dbContext.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            if (entity is not null)
            {
                _dbContext.Entry(entity).State =
                    EntityState.Detached;
            }

            throw;
        }
        catch (Exception ex)
        {
            if (entity is not null)
            {
                _dbContext.Entry(entity).State =
                    EntityState.Detached;
            }

            throw new PdgException(
                PdgErrorCategory.AuditWriteFailed,
                ex);
        }
    }
}
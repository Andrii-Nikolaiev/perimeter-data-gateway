using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Infrastructure.PlatformStore;

public sealed class PlatformStoreRepository : IPlatformStore
{
    private readonly PlatformStoreDbContext _dbContext;

    public PlatformStoreRepository(
        PlatformStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Subject?> GetSubjectAsync(
        string subjectId,
        CancellationToken ct)
    {
        var entity = await ExecuteQueryAsync(
            () => _dbContext.Subjects
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.SubjectId == subjectId,
                    ct),
            ct);

        return entity is null
            ? null
            : new Subject(
                entity.SubjectId,
                entity.RoleCode);
    }

    public async Task<Actor?> GetActorAsync(
        string actorId,
        CancellationToken ct)
    {
        var entity = await ExecuteQueryAsync(
            () => _dbContext.Actors
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ActorId == actorId,
                    ct),
            ct);

        return entity is null
            ? null
            : new Actor(
                entity.ActorId,
                entity.ActorType);
    }

    public async Task<SubjectActorDelegation?> GetDelegationAsync(
        string subjectId,
        string actorId,
        CancellationToken ct)
    {
        var entity = await ExecuteQueryAsync(
            () => _dbContext.Delegations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.SubjectId == subjectId &&
                        x.ActorId == actorId,
                    ct),
            ct);

        return entity is null
            ? null
            : new SubjectActorDelegation(
                entity.SubjectId,
                entity.ActorId,
                entity.IsActive);
    }

    public async Task<IReadOnlySet<string>> GetActorCapabilitiesAsync(
        string actorId,
        CancellationToken ct)
    {
        var capabilities = await ExecuteQueryAsync(
            () => _dbContext.ActorCapabilities
                .AsNoTracking()
                .Where(x => x.ActorId == actorId)
                .Select(x => x.Capability)
                .ToListAsync(ct),
            ct);

        return new HashSet<string>(
            capabilities,
            StringComparer.Ordinal);
    }

    public async Task<PublishedResource?> GetPublishedResourceAsync(
        string resourceName,
        CancellationToken ct)
    {
        var resource = await ExecuteQueryAsync(
            () => _dbContext.Resources
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ResourceName == resourceName,
                    ct),
            ct);

        if (resource is null)
        {
            return null;
        }

        var parameterEntities = await ExecuteQueryAsync(
            () => _dbContext.ResourceParameters
                .AsNoTracking()
                .Where(x => x.ResourceName == resourceName)
                .OrderBy(x => x.ParamName)
                .ToListAsync(ct),
            ct);

        var outputFieldEntities = await ExecuteQueryAsync(
            () => _dbContext.ResourceOutputFields
                .AsNoTracking()
                .Where(x => x.ResourceName == resourceName)
                .OrderBy(x => x.Ordinal)
                .ToListAsync(ct),
            ct);

        var parameters = parameterEntities
            .Select(x => new ResourceParameter(
                x.ParamName,
                x.ParamType,
                x.Required))
            .ToArray();

        var outputFields = outputFieldEntities
            .Select(x => new ResourceOutputField(
                x.FieldName,
                x.Ordinal))
            .ToArray();

        return new PublishedResource(
            resource.ResourceName,
            resource.RequiredCapability,
            resource.MaxRows,
            parameters,
            outputFields);
    }

    public async Task<SubjectResourcePermission?>
        GetSubjectResourcePermissionAsync(
            string subjectId,
            string resourceName,
            CancellationToken ct)
    {
        var entity = await ExecuteQueryAsync(
            () => _dbContext.SubjectResourcePermissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.SubjectId == subjectId &&
                        x.ResourceName == resourceName,
                    ct),
            ct);

        if (entity is null)
        {
            return null;
        }

        var rowScopeMode = entity.RowScopeMode switch
        {
            "ALL" => RowScopeMode.All,
            "ALLOW_LIST" => RowScopeMode.AllowList,
            _ => throw new PdgException(
                PdgErrorCategory.InternalError)
        };

        return new SubjectResourcePermission(
            entity.SubjectId,
            entity.ResourceName,
            entity.Allowed,
            rowScopeMode);
    }

    public async Task<IReadOnlySet<string>>
        GetSubjectRowScopeValuesAsync(
            string subjectId,
            string resourceName,
            string dimension,
            CancellationToken ct)
    {
        var values = await ExecuteQueryAsync(
            () => _dbContext.SubjectRowScopes
                .AsNoTracking()
                .Where(x =>
                    x.SubjectId == subjectId &&
                    x.ResourceName == resourceName &&
                    x.Dimension == dimension)
                .Select(x => x.AllowedValue)
                .ToListAsync(ct),
            ct);

        return new HashSet<string>(
            values,
            StringComparer.Ordinal);
    }

    private static async Task<T> ExecuteQueryAsync<T>(
        Func<Task<T>> query,
        CancellationToken ct)
    {
        try
        {
            return await query();
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (PdgException)
        {
            throw;
        }
        catch (Exception ex)
            when (ContainsNpgsqlException(ex))
        {
            throw new PdgException(
                PdgErrorCategory.PlatformStoreUnavailable,
                ex);
        }
    }

    private static bool ContainsNpgsqlException(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is NpgsqlException)
            {
                return true;
            }
        }

        return false;
    }
}

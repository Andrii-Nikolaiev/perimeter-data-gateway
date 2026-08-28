using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Abstractions;

public interface IPlatformStore
{
    Task<Subject?> GetSubjectAsync(
        string subjectId,
        CancellationToken ct);

    Task<Actor?> GetActorAsync(
        string actorId,
        CancellationToken ct);

    Task<SubjectActorDelegation?> GetDelegationAsync(
        string subjectId,
        string actorId,
        CancellationToken ct);

    Task<IReadOnlySet<string>> GetActorCapabilitiesAsync(
        string actorId,
        CancellationToken ct);

    Task<PublishedResource?> GetPublishedResourceAsync(
        string resourceName,
        CancellationToken ct);

    Task<SubjectResourcePermission?> GetSubjectResourcePermissionAsync(
        string subjectId,
        string resourceName,
        CancellationToken ct);

    Task<IReadOnlySet<string>> GetSubjectRowScopeValuesAsync(
        string subjectId,
        string resourceName,
        string dimension,
        CancellationToken ct);
}
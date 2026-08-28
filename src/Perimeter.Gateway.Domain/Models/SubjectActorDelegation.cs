namespace Perimeter.Gateway.Domain.Models;

public sealed record SubjectActorDelegation(
    string SubjectId,
    string ActorId,
    bool IsActive);
namespace Perimeter.Gateway.Domain.Models;

public sealed record ValidatedTokenContext(
    string SubjectId,
    string ActorId,
    IReadOnlySet<string> Scopes);
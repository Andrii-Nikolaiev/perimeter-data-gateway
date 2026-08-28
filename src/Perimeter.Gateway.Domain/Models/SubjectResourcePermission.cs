namespace Perimeter.Gateway.Domain.Models;

public sealed record SubjectResourcePermission(
    string SubjectId,
    string ResourceName,
    bool Allowed,
    RowScopeMode RowScopeMode);
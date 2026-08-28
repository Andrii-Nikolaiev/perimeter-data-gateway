namespace Perimeter.Gateway.Domain.Models;

public sealed record AuditRecord(
    DateTimeOffset Timestamp,
    string Subject,
    string Actor,
    string Capability,
    string Resource,
    string Scope,
    string Decision,
    string ReasonCategory,
    IReadOnlyDictionary<string, string?> NormalizedParameters,
    RowScope? EffectiveRowScope,
    int RowsReturned);
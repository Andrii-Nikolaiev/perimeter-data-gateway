namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class AuditRecordEntity
{
    public long AuditId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string SubjectId { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;

    public string Capability { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Decision { get; set; } = string.Empty;

    public string ReasonCategory { get; set; } = string.Empty;

    public string NormalizedParameters { get; set; } = "{}";

    public string? EffectiveRowScope { get; set; }

    public int RowsReturned { get; set; }
}
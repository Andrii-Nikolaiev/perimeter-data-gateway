namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class SubjectRowScopeEntity
{
    public string SubjectId { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;

    public string Dimension { get; set; } = string.Empty;

    public string AllowedValue { get; set; } = string.Empty;
}
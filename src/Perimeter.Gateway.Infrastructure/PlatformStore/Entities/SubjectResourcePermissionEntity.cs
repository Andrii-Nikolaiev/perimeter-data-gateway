namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class SubjectResourcePermissionEntity
{
    public string SubjectId { get; set; } = string.Empty;

    public string ResourceName { get; set; } = string.Empty;

    public bool Allowed { get; set; }

    public string RowScopeMode { get; set; } = string.Empty;
}
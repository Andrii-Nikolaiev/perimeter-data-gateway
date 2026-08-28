namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class ResourceEntity
{
    public string ResourceName { get; set; } = string.Empty;

    public string RequiredCapability { get; set; } = string.Empty;

    public int MaxRows { get; set; }
}
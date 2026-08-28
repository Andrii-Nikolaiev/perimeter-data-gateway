namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class ResourceParameterEntity
{
    public string ResourceName { get; set; } = string.Empty;

    public string ParamName { get; set; } = string.Empty;

    public string ParamType { get; set; } = string.Empty;

    public bool Required { get; set; }
}
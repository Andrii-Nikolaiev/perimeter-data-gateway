namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class ResourceOutputFieldEntity
{
    public string ResourceName { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public int Ordinal { get; set; }
}
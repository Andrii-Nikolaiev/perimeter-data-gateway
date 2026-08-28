namespace Perimeter.Gateway.Infrastructure.PlatformStore.Entities;

public sealed class DelegationEntity
{
    public string SubjectId { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
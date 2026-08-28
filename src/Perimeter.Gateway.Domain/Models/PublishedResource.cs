namespace Perimeter.Gateway.Domain.Models;

public sealed record PublishedResource(
    string ResourceName,
    string RequiredCapability,
    int MaxRows,
    IReadOnlyList<ResourceParameter> Parameters,
    IReadOnlyList<ResourceOutputField> OutputFields);
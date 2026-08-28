namespace Perimeter.Gateway.Domain.Models;

public sealed record ResourceParameter(
    string Name,
    string Type,
    bool Required);
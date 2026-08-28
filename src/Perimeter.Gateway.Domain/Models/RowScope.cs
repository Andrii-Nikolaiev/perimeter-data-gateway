namespace Perimeter.Gateway.Domain.Models;

public sealed record RowScope(
    RowScopeMode Mode,
    IReadOnlyDictionary<string, IReadOnlySet<string>> Dimensions);
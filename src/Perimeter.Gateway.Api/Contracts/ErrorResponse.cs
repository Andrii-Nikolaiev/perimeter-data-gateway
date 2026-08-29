namespace Perimeter.Gateway.Api.Contracts;

public sealed record ErrorResponse(
    int Status,
    string Category);
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Resources;

public sealed record GetSalesSummaryRequest(
    ValidatedTokenContext Token,
    string ResourceName,
    string? Country);
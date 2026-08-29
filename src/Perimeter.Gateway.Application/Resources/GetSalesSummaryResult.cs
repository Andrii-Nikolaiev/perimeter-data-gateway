using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Resources;

public sealed record GetSalesSummaryResult(
    IReadOnlyList<SalesSummaryRow> Rows,
    int Limit);
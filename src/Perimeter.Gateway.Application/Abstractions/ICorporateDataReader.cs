using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Abstractions;

public interface ICorporateDataReader
{
    Task<IReadOnlyList<SalesSummaryRow>> ReadSalesSummaryAsync(
        RowScope effectiveScope,
        int take,
        CancellationToken ct);
}
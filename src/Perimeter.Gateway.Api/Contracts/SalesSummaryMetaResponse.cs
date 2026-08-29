namespace Perimeter.Gateway.Api.Contracts;

public sealed record SalesSummaryMetaResponse(
    int RowsReturned,
    int Limit);
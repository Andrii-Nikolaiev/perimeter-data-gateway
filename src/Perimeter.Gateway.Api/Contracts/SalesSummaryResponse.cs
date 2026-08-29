namespace Perimeter.Gateway.Api.Contracts;

public sealed record SalesSummaryResponse(
    int Status,
    string Category,
    IReadOnlyList<SalesSummaryItemResponse> Data,
    SalesSummaryMetaResponse Meta);

public sealed record SalesSummaryItemResponse(
    int CustomerId,
    string Country,
    DateOnly InvoiceDate,
    decimal Total);
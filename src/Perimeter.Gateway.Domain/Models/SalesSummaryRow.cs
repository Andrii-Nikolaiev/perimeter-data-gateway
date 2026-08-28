namespace Perimeter.Gateway.Domain.Models;

public sealed record SalesSummaryRow(
    int CustomerId,
    string Country,
    DateOnly InvoiceDate,
    decimal Total);
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Resources;

public sealed class SalesSummaryRequestValidator
{
    private const string CountryParameter = "country";

    public GetSalesSummaryRequest Validate(
        ValidatedTokenContext token,
        string resourceName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> parameters)
    {
        string? country = null;

        foreach (var parameter in parameters)
        {
            if (!string.Equals(
                    parameter.Key,
                    CountryParameter,
                    StringComparison.Ordinal))
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }

            if (parameter.Value is null ||
                parameter.Value.Count != 1)
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }

            var value = parameter.Value[0];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }

            country = value.Trim();
        }

        return new GetSalesSummaryRequest(
            token,
            resourceName,
            country);
    }
}
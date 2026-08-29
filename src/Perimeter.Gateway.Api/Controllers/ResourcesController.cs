using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perimeter.Gateway.Api.Authentication;
using Perimeter.Gateway.Api.Contracts;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Application.Resources;

namespace Perimeter.Gateway.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/resources")]
public sealed class ResourcesController : ControllerBase
{
    private readonly ValidatedTokenContextFactory _tokenContextFactory;
    private readonly SalesSummaryRequestValidator _requestValidator;
    private readonly GetSalesSummaryHandler _handler;

    public ResourcesController(
        ValidatedTokenContextFactory tokenContextFactory,
        SalesSummaryRequestValidator requestValidator,
        GetSalesSummaryHandler handler)
    {
        _tokenContextFactory = tokenContextFactory;
        _requestValidator = requestValidator;
        _handler = handler;
    }

    [HttpGet("{resourceName}")]
    public async Task<ActionResult<SalesSummaryResponse>> GetAsync(
        string resourceName,
        CancellationToken ct)
    {
        if (!_tokenContextFactory.TryCreate(
                User,
                out var tokenContext) ||
            tokenContext is null)
        {
            throw new PdgException(
                PdgErrorCategory.AuthenticationFailed);
        }

        var parameters =
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.Ordinal);

        foreach (var parameter in Request.Query)
        {
            parameters[parameter.Key] =
                parameter.Value
                    .Select(value => value ?? string.Empty)
                    .ToArray();
        }

        var request =
            _requestValidator.Validate(
                tokenContext,
                resourceName,
                parameters);

        var result =
            await _handler.HandleAsync(
                request,
                ct);

        var data =
            result.Rows
                .Select(row =>
                    new SalesSummaryItemResponse(
                        row.CustomerId,
                        row.Country,
                        row.InvoiceDate,
                        row.Total))
                .ToArray();

        return Ok(
            new SalesSummaryResponse(
                StatusCodes.Status200OK,
                "success",
                data,
                new SalesSummaryMetaResponse(
                    data.Length,
                    result.Limit)));
    }
}
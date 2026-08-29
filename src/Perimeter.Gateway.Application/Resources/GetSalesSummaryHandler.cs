using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Audit;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Resources;

public sealed class GetSalesSummaryHandler
{
    private readonly IAccessPolicyEvaluator _accessPolicyEvaluator;
    private readonly IPlatformStore _platformStore;
    private readonly ICorporateDataReader _corporateDataReader;
    private readonly IAuditWriter _auditWriter;
    private readonly AuditRecordFactory _auditRecordFactory;

    public GetSalesSummaryHandler(
        IAccessPolicyEvaluator accessPolicyEvaluator,
        IPlatformStore platformStore,
        ICorporateDataReader corporateDataReader,
        IAuditWriter auditWriter,
        AuditRecordFactory auditRecordFactory)
    {
        _accessPolicyEvaluator = accessPolicyEvaluator;
        _platformStore = platformStore;
        _corporateDataReader = corporateDataReader;
        _auditWriter = auditWriter;
        _auditRecordFactory = auditRecordFactory;
    }

    public async Task<GetSalesSummaryResult> HandleAsync(
        GetSalesSummaryRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedParameters =
            BuildNormalizedParameters(request);

        var resourceSnapshot =
            await _platformStore.GetPublishedResourceAsync(
                request.ResourceName,
                ct);

        var decision =
            await _accessPolicyEvaluator.EvaluateAsync(
                request.Token,
                request.ResourceName,
                normalizedParameters,
                ct);

        if (decision.Decision == AuthorizationDecisionKind.Deny)
        {
            var denyAudit =
                _auditRecordFactory.Create(
                    request.Token,
                    decision,
                    normalizedParameters,
                    decision.ReasonCategory,
                    0);

            await _auditWriter.WriteAsync(
                denyAudit,
                ct);

            throw new PdgException(
                PdgErrorCategory.AccessDenied);
        }

        if (decision.Decision != AuthorizationDecisionKind.Allow ||
            decision.EffectiveRowScope is null)
        {
            throw new PdgException(
                PdgErrorCategory.InternalError);
        }

        if (resourceSnapshot is null ||
            resourceSnapshot.MaxRows <= 0 ||
            resourceSnapshot.MaxRows == int.MaxValue)
        {
            throw new PdgException(
                PdgErrorCategory.InternalError);
        }

        var take = resourceSnapshot.MaxRows + 1;

        IReadOnlyList<SalesSummaryRow> bufferedRows;

        try
        {
            bufferedRows =
                await _corporateDataReader.ReadSalesSummaryAsync(
                    decision.EffectiveRowScope,
                    take,
                    ct);
        }
        catch (PdgException ex)
            when (ex.Category ==
                  PdgErrorCategory.CorporateDataSourceUnavailable)
        {
            var failureAudit =
                _auditRecordFactory.Create(
                    request.Token,
                    decision,
                    normalizedParameters,
                    PdgErrorCategory.CorporateDataSourceUnavailable,
                    0);

            await _auditWriter.WriteAsync(
                failureAudit,
                ct);

            throw;
        }

        if (bufferedRows.Count > resourceSnapshot.MaxRows)
        {
            var limitAudit =
                _auditRecordFactory.Create(
                    request.Token,
                    decision,
                    normalizedParameters,
                    PdgErrorCategory.ResultLimitExceeded,
                    0);

            await _auditWriter.WriteAsync(
                limitAudit,
                ct);

            throw new PdgException(
                PdgErrorCategory.ResultLimitExceeded);
        }

        var allowAudit =
            _auditRecordFactory.Create(
                request.Token,
                decision,
                normalizedParameters,
                decision.ReasonCategory,
                bufferedRows.Count);

        await _auditWriter.WriteAsync(
            allowAudit,
            ct);

        return new GetSalesSummaryResult(
            bufferedRows,
            resourceSnapshot.MaxRows);
    }

    private static IReadOnlyDictionary<string, string?>
        BuildNormalizedParameters(
            GetSalesSummaryRequest request)
    {
        var parameters =
            new Dictionary<string, string?>(
                StringComparer.Ordinal);

        if (request.Country is not null)
        {
            parameters["country"] = request.Country;
        }

        return parameters;
    }
}
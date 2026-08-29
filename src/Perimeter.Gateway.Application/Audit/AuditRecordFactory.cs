using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Audit;

public sealed class AuditRecordFactory
{
    private readonly IClock _clock;

    public AuditRecordFactory(IClock clock)
    {
        _clock = clock;
    }

    public AuditRecord Create(
        ValidatedTokenContext token,
        AuthorizationDecision decision,
        IReadOnlyDictionary<string, string?> normalizedParameters,
        string reasonCategory,
        int rowsReturned)
    {
        if (rowsReturned < 0)
        {
            throw new PdgException(
                PdgErrorCategory.InternalError);
        }

        if (decision.Decision == AuthorizationDecisionKind.Deny &&
            rowsReturned != 0)
        {
            throw new PdgException(
                PdgErrorCategory.InternalError);
        }

        var decisionText = decision.Decision switch
        {
            AuthorizationDecisionKind.Allow => "ALLOW",
            AuthorizationDecisionKind.Deny => "DENY",
            _ => throw new PdgException(
                PdgErrorCategory.InternalError)
        };

        var scope = string.Join(
            " ",
            token.Scopes.OrderBy(
                value => value,
                StringComparer.Ordinal));

        var parameters =
            normalizedParameters
                .OrderBy(
                    pair => pair.Key,
                    StringComparer.Ordinal)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal);

        return new AuditRecord(
            _clock.UtcNow,
            token.SubjectId,
            token.ActorId,
            decision.Capability,
            decision.ResourceName,
            scope,
            decisionText,
            reasonCategory,
            parameters,
            decision.EffectiveRowScope,
            rowsReturned);
    }
}
namespace Perimeter.Gateway.Domain.Models;

public sealed record AuthorizationDecision(
    AuthorizationDecisionKind Decision,
    string Capability,
    string ResourceName,
    RowScope? EffectiveRowScope,
    string ReasonCategory);
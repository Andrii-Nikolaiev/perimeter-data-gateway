using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Abstractions;

public interface IAccessPolicyEvaluator
{
    Task<AuthorizationDecision> EvaluateAsync(
        ValidatedTokenContext token,
        string resourceName,
        IReadOnlyDictionary<string, string?> parameters,
        CancellationToken ct);
}
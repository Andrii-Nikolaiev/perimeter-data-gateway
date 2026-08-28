using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Authorization;

public sealed class AuthorizationPolicyEvaluator : IAccessPolicyEvaluator
{
    private const string CountryDimension = "country";

    private readonly IPlatformStore _platformStore;

    public AuthorizationPolicyEvaluator(IPlatformStore platformStore)
    {
        _platformStore = platformStore;
    }

    public async Task<AuthorizationDecision> EvaluateAsync(
        ValidatedTokenContext token,
        string resourceName,
        IReadOnlyDictionary<string, string?> parameters,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(parameters);

        // 1. Subject-Actor delegation.
        var delegation = await _platformStore.GetDelegationAsync(
            token.SubjectId,
            token.ActorId,
            ct);

        if (delegation is null || !delegation.IsActive)
        {
            return Deny(
                GetRequestedCapability(token),
                resourceName,
                AuthorizationReasonCategories.DelegationNotAllowed);
        }

        // 2. Actor policy limits.
        var actor = await _platformStore.GetActorAsync(token.ActorId, ct);

        if (actor is null)
        {
            throw new PdgException(PdgErrorCategory.InternalError);
        }

        var actorCapabilities =
            await _platformStore.GetActorCapabilitiesAsync(token.ActorId, ct);

        if (actorCapabilities is null)
        {
            throw new PdgException(PdgErrorCategory.InternalError);
        }

        var disallowedScope = token.Scopes
            .FirstOrDefault(scope => !actorCapabilities.Contains(scope));

        if (disallowedScope is not null)
        {
            return Deny(
                disallowedScope,
                resourceName,
                AuthorizationReasonCategories.ActorCapabilityNotAllowed);
        }

        // 3. Subject authorization state.
        var subject = await _platformStore.GetSubjectAsync(
            token.SubjectId,
            ct);

        if (subject is null)
        {
            throw new PdgException(PdgErrorCategory.InternalError);
        }

        // 4. Published Resource resolution.
        var resource = await _platformStore.GetPublishedResourceAsync(
            resourceName,
            ct);

        if (resource is null)
        {
            throw new PdgException(PdgErrorCategory.ResourceNotFound);
        }

        // 5. Subject -> Resource permission.
        var permission =
            await _platformStore.GetSubjectResourcePermissionAsync(
                token.SubjectId,
                resource.ResourceName,
                ct);

        if (permission is null || !permission.Allowed)
        {
            return Deny(
                resource.RequiredCapability,
                resource.ResourceName,
                AuthorizationReasonCategories.SubjectResourceNotAllowed);
        }

        // 6. Required capability of the Published Resource.
        if (!token.Scopes.Contains(resource.RequiredCapability))
        {
            return Deny(
                resource.RequiredCapability,
                resource.ResourceName,
                AuthorizationReasonCategories.RequiredCapabilityNotAllowed);
        }

        if (!actorCapabilities.Contains(resource.RequiredCapability))
        {
            return Deny(
                resource.RequiredCapability,
                resource.ResourceName,
                AuthorizationReasonCategories.ActorCapabilityNotAllowed);
        }

        // 7. Published Resource parameter contract.
        ValidateParameters(resource, parameters);

        parameters.TryGetValue(
            CountryDimension,
            out var requestedCountry);

        // 8. Effective row scope.
        return permission.RowScopeMode switch
        {
            RowScopeMode.All => BuildAllScopeDecision(
                resource,
                requestedCountry),

            RowScopeMode.AllowList => await BuildAllowListDecisionAsync(
                token,
                resource,
                requestedCountry,
                ct),

            _ => throw new PdgException(PdgErrorCategory.InternalError)
        };
    }

    private async Task<AuthorizationDecision> BuildAllowListDecisionAsync(
        ValidatedTokenContext token,
        PublishedResource resource,
        string? requestedCountry,
        CancellationToken ct)
    {
        var storedValues =
            await _platformStore.GetSubjectRowScopeValuesAsync(
                token.SubjectId,
                resource.ResourceName,
                CountryDimension,
                ct);

        if (storedValues is null || storedValues.Count == 0)
        {
            throw new PdgException(PdgErrorCategory.InternalError);
        }

        var allowedCountries = new HashSet<string>(
            storedValues,
            StringComparer.Ordinal);

        if (requestedCountry is not null &&
            !allowedCountries.Contains(requestedCountry))
        {
            return Deny(
                resource.RequiredCapability,
                resource.ResourceName,
                AuthorizationReasonCategories.RowScopeNotAllowed);
        }

        IReadOnlySet<string> effectiveCountries =
            requestedCountry is null
                ? allowedCountries
                : new HashSet<string>(
                    new[] { requestedCountry },
                    StringComparer.Ordinal);

        var dimensions =
            new Dictionary<string, IReadOnlySet<string>>(
                StringComparer.Ordinal)
            {
                [CountryDimension] = effectiveCountries
            };

        var effectiveScope = new RowScope(
            RowScopeMode.AllowList,
            dimensions);

        return Allow(resource, effectiveScope);
    }

    private static AuthorizationDecision BuildAllScopeDecision(
        PublishedResource resource,
        string? requestedCountry)
    {
        if (requestedCountry is null)
        {
            var emptyDimensions =
                new Dictionary<string, IReadOnlySet<string>>(
                    StringComparer.Ordinal);

            return Allow(
                resource,
                new RowScope(
                    RowScopeMode.All,
                    emptyDimensions));
        }

        var requestedValues = new HashSet<string>(
            new[] { requestedCountry },
            StringComparer.Ordinal);

        var dimensions =
            new Dictionary<string, IReadOnlySet<string>>(
                StringComparer.Ordinal)
            {
                [CountryDimension] = requestedValues
            };

        return Allow(
            resource,
            new RowScope(
                RowScopeMode.AllowList,
                dimensions));
    }

    private static void ValidateParameters(
        PublishedResource resource,
        IReadOnlyDictionary<string, string?> parameters)
    {
        foreach (var parameter in parameters)
        {
            var definition = resource.Parameters.FirstOrDefault(
                configured =>
                    string.Equals(
                        configured.Name,
                        parameter.Key,
                        StringComparison.Ordinal));

            if (definition is null)
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }

            if (string.IsNullOrWhiteSpace(parameter.Value))
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }
        }

        foreach (var definition in resource.Parameters)
        {
            if (!definition.Required)
            {
                continue;
            }

            if (!parameters.TryGetValue(
                    definition.Name,
                    out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                throw new PdgException(
                    PdgErrorCategory.InvalidRequest);
            }
        }
    }

    private static AuthorizationDecision Allow(
        PublishedResource resource,
        RowScope effectiveScope)
    {
        return new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            resource.RequiredCapability,
            resource.ResourceName,
            effectiveScope,
            AuthorizationReasonCategories.Authorized);
    }

    private static AuthorizationDecision Deny(
        string capability,
        string resourceName,
        string reasonCategory)
    {
        return new AuthorizationDecision(
            AuthorizationDecisionKind.Deny,
            capability,
            resourceName,
            null,
            reasonCategory);
    }

    private static string GetRequestedCapability(
        ValidatedTokenContext token)
    {
        if (token.Scopes.Count == 0)
        {
            return string.Empty;
        }

        if (token.Scopes.Count == 1)
        {
            return token.Scopes.First();
        }

        return string.Join(
            " ",
            token.Scopes.OrderBy(
                scope => scope,
                StringComparer.Ordinal));
    }
}
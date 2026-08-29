using System.Security.Claims;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Api.Authentication;

public sealed class ValidatedTokenContextFactory
{
    private readonly JwtActorClaimsValidator _claimsValidator;

    public ValidatedTokenContextFactory(
        JwtActorClaimsValidator claimsValidator)
    {
        _claimsValidator = claimsValidator;
    }

    public bool TryCreate(
        ClaimsPrincipal principal,
        out ValidatedTokenContext? tokenContext)
    {
        ArgumentNullException.ThrowIfNull(principal);

        tokenContext = null;

        if (!_claimsValidator.TryValidate(
                principal,
                out var subjectId,
                out var actorId))
        {
            return false;
        }

        var scopes = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var scopeClaim in principal.FindAll("scope"))
        {
            var values = scopeClaim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (var value in values)
            {
                scopes.Add(value);
            }
        }

        tokenContext = new ValidatedTokenContext(
            subjectId,
            actorId,
            scopes);

        return true;
    }
}
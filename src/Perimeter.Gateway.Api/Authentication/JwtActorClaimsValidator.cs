using System.Security.Claims;
using System.Text.Json;

namespace Perimeter.Gateway.Api.Authentication;

public sealed class JwtActorClaimsValidator
{
    public bool TryValidate(
        ClaimsPrincipal principal,
        out string subjectId,
        out string actorId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        subjectId = string.Empty;
        actorId = string.Empty;

        var subjectClaims =
            principal.FindAll("sub").ToArray();

        if (subjectClaims.Length != 1 ||
            string.IsNullOrWhiteSpace(subjectClaims[0].Value))
        {
            return false;
        }

        var actorClaims =
            principal.FindAll("act").ToArray();

        if (actorClaims.Length != 1 ||
            string.IsNullOrWhiteSpace(actorClaims[0].Value))
        {
            return false;
        }

        string? actorSubject;

        try
        {
            using var document =
                JsonDocument.Parse(actorClaims[0].Value);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Object)
            {
                return false;
            }

            var actorSubjectProperties =
                document.RootElement
                    .EnumerateObject()
                    .Where(property =>
                        property.NameEquals("sub"))
                    .ToArray();

            if (actorSubjectProperties.Length != 1 ||
                actorSubjectProperties[0].Value.ValueKind !=
                    JsonValueKind.String)
            {
                return false;
            }

            actorSubject =
                actorSubjectProperties[0].Value.GetString();
        }
        catch (JsonException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(actorSubject))
        {
            return false;
        }

        subjectId = subjectClaims[0].Value;
        actorId = actorSubject;

        return true;
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Perimeter.Gateway.AcceptanceTests.Helpers;

public sealed class JwtTestTokenFactory
{
    private const string DefaultSubjectId = "user_42";
    private const string DefaultActorId = "sales_copilot_v1";
    private const string DefaultScope = "sales.read";

    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKey;

    public JwtTestTokenFactory(
        string issuer,
        string audience,
        string signingKey)
    {
        _issuer = issuer;
        _audience = audience;
        _signingKey = signingKey;
    }

    public string CreateValidToken(
        string subjectId = DefaultSubjectId,
        string actorId = DefaultActorId,
        string scope = DefaultScope)
    {
        return CreateToken(
            subjectId,
            actorId,
            scope,
            DateTimeOffset.UtcNow.AddMinutes(5));
    }

    public string CreateExpiredToken(
        string subjectId = DefaultSubjectId,
        string actorId = DefaultActorId,
        string scope = DefaultScope)
    {
        return CreateToken(
            subjectId,
            actorId,
            scope,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            notBefore:
                DateTimeOffset.UtcNow.AddMinutes(-10));
    }

    public string CreateMissingActorToken(
        string subjectId = DefaultSubjectId,
        string scope = DefaultScope)
    {
        return CreateToken(
            subjectId,
            actorId: null,
            scope,
            DateTimeOffset.UtcNow.AddMinutes(5));
    }

    public string CreateMissingScopeToken(
        string subjectId = DefaultSubjectId,
        string actorId = DefaultActorId)
    {
        return CreateToken(
            subjectId,
            actorId,
            scope: null,
            DateTimeOffset.UtcNow.AddMinutes(5));
    }

    public string CreateToken(
        string subjectId,
        string? actorId,
        string? scope,
        DateTimeOffset expires,
        DateTimeOffset? notBefore = null,
        string? issuerOverride = null,
        string? audienceOverride = null,
        string? signingKeyOverride = null)
    {
        var claims = new List<Claim>
        {
            new("sub", subjectId)
        };

        if (actorId is not null)
        {
            var actorJson =
                JsonSerializer.Serialize(
                    new Dictionary<string, string>
                    {
                        ["sub"] = actorId
                    });

            claims.Add(
                new Claim(
                    "act",
                    actorJson,
                    JsonClaimValueTypes.Json));
        }

        if (scope is not null)
        {
            claims.Add(
                new Claim(
                    "scope",
                    scope));
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    signingKeyOverride ?? _signingKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuerOverride ?? _issuer,
                audienceOverride ?? _audience,
                claims,
                notBefore:
                    (notBefore ?? DateTimeOffset.UtcNow)
                        .UtcDateTime,
                expires:
                    expires.UtcDateTime,
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}

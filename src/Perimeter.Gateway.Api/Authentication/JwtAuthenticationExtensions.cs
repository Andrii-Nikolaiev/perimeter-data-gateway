using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Perimeter.Gateway.Application.Errors;

namespace Perimeter.Gateway.Api.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddPdgJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var signingKey = configuration["JWT_SIGNING_KEY"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Jwt:Audience configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT_SIGNING_KEY configuration is required.");
        }

        services.AddSingleton<JwtActorClaimsValidator>();
        services.AddSingleton<ValidatedTokenContextFactory>();

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey));

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        RequireSignedTokens = true,
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = securityKey,
                        ValidAlgorithms =
                            new[]
                            {
                                SecurityAlgorithms.HmacSha256
                            },
                        ClockSkew = TimeSpan.Zero
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var validator =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    JwtActorClaimsValidator>();

                        if (context.Principal is null ||
                            !validator.TryValidate(
                                context.Principal,
                                out _,
                                out _))
                        {
                            context.Fail(
                                PdgErrorCategory.AuthenticationFailed);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
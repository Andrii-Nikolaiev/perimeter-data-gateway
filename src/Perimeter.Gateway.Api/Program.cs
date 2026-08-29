using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Perimeter.Gateway.Api;
using Perimeter.Gateway.Api.Authentication;
using Perimeter.Gateway.Api.Health;
using Perimeter.Gateway.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddPdgServices(builder.Configuration);
builder.Services.AddPdgJwtAuthentication(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddCheck<PlatformStoreHealthCheck>(
        "platform-store",
        tags: new[] { "ready" })
    .AddCheck<CorporateDataHealthCheck>(
        "corporate-data-source",
        tags: new[] { "ready" })
    .AddCheck<RequiredConfigurationHealthCheck>(
        "required-configuration",
        tags: new[] { "ready" });

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains("ready")
    });

app.Run();

using Perimeter.Gateway.Api;
using Perimeter.Gateway.Api.Authentication;
using Perimeter.Gateway.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddPdgServices(builder.Configuration);
builder.Services.AddPdgJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
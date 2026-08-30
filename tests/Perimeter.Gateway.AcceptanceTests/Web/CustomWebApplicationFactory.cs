using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Perimeter.Gateway.Api.Controllers;

namespace Perimeter.Gateway.AcceptanceTests.Web;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<ResourcesController>
{
    private readonly string _platformStoreConnectionString;
    private readonly string _corporateDataSourceConnectionString;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly string _jwtSigningKey;

    public CustomWebApplicationFactory(
        string platformStoreConnectionString,
        string corporateDataSourceConnectionString,
        string jwtIssuer,
        string jwtAudience,
        string jwtSigningKey)
    {
        _platformStoreConnectionString =
            platformStoreConnectionString;
        _corporateDataSourceConnectionString =
            corporateDataSourceConnectionString;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
        _jwtSigningKey = jwtSigningKey;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Acceptance");

        builder.UseSetting(
            "ConnectionStrings:PlatformStore",
            _platformStoreConnectionString);

        builder.UseSetting(
            "ConnectionStrings:CorporateDataSource",
            _corporateDataSourceConnectionString);

        builder.UseSetting(
            "Jwt:Issuer",
            _jwtIssuer);

        builder.UseSetting(
            "Jwt:Audience",
            _jwtAudience);

        builder.UseSetting(
            "Jwt:SigningKey",
            _jwtSigningKey);
    }
}

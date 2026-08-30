using System.Security.Cryptography;
using Perimeter.Gateway.AcceptanceTests.Web;
using Perimeter.Gateway.IntegrationTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.Fixtures;

public sealed class AcceptanceEnvironment : IAsyncLifetime
{
    public const string JwtIssuer =
        "https://pdg.local/test-issuer";

    public const string JwtAudience =
        "pdg-api";

    private CustomWebApplicationFactory? _factory;
    private HttpClient? _client;
    private bool _platformInitialized;
    private bool _corporateInitialized;
    private bool _disposed;

    public AcceptanceEnvironment()
    {
        JwtSigningKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

        PlatformStore =
            new PlatformStoreIntegrationFixture();

        CorporateData =
            new CorporateDataIntegrationFixture();
    }

    public string JwtSigningKey { get; }

    public PlatformStoreIntegrationFixture PlatformStore { get; }

    public CorporateDataIntegrationFixture CorporateData { get; }

    public HttpClient Client =>
        _client
        ?? throw new InvalidOperationException(
            "Acceptance HTTP client is not initialized.");

    public CustomWebApplicationFactory Factory =>
        _factory
        ?? throw new InvalidOperationException(
            "Acceptance web application factory is not initialized.");

    public async ValueTask InitializeAsync()
    {
        try
        {
            await PlatformStore.InitializeAsync();
            _platformInitialized = true;

            await CorporateData.InitializeAsync();
            _corporateInitialized = true;

            _factory =
                new CustomWebApplicationFactory(
                    PlatformStore.RuntimeConnectionString,
                    CorporateData.RuntimeConnectionString,
                    JwtIssuer,
                    JwtAudience,
                    JwtSigningKey);

            _client = _factory.CreateClient();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public void RecreateWebHost()
    {
        _client?.Dispose();
        _factory?.Dispose();

        _client = null;
        _factory = null;

        _factory =
            new CustomWebApplicationFactory(
                PlatformStore.RuntimeConnectionString,
                CorporateData.RuntimeConnectionString,
                JwtIssuer,
                JwtAudience,
                JwtSigningKey);

        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _client?.Dispose();
        _factory?.Dispose();

        if (_corporateInitialized)
        {
            await CorporateData.DisposeAsync();
        }

        if (_platformInitialized)
        {
            await PlatformStore.DisposeAsync();
        }
    }
}
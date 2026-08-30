using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T21;

[Collection(AcceptanceCollection.Name)]
public sealed class MalformedPolicyTests
{
    private readonly AcceptanceEnvironment _environment;

    public MalformedPolicyTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T21_Allow_list_without_row_scope_returns_500_internal_error()
    {
        var ct =
            TestContext.Current.CancellationToken;

        var tokenFactory =
            new JwtTestTokenFactory(
                AcceptanceEnvironment.JwtIssuer,
                AcceptanceEnvironment.JwtAudience,
                _environment.JwtSigningKey);

        var databaseMutator =
            new AcceptanceDatabaseMutator(
                _environment);

        await databaseMutator.RemoveSubjectRowScopeAsync(ct);

        try
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/api/resources/SalesSummary");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    tokenFactory.CreateValidToken(
                        subjectId: "user_42"));

            using var response =
                await _environment.Client.SendAsync(
                    request,
                    ct);

            await ResponseAssertions.AssertErrorAsync(
                response,
                HttpStatusCode.InternalServerError,
                "internal_error",
                ct);
        }
        finally
        {
            await databaseMutator.RestoreSubjectRowScopeAsync(ct);
        }
    }
}
using System.Net;
using System.Net.Http.Headers;
using Perimeter.Gateway.AcceptanceTests.Fixtures;
using Perimeter.Gateway.AcceptanceTests.Helpers;

namespace Perimeter.Gateway.AcceptanceTests.T17;

[Collection(AcceptanceCollection.Name)]
public sealed class ResultLimitExceededTests
{
    private const int MaxRows = 500;

    private readonly AcceptanceEnvironment _environment;

    public ResultLimitExceededTests(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    [Fact]
    public async Task T17_Result_above_max_rows_returns_400_result_limit_exceeded()
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

        var insertedRows =
            await databaseMutator
                .AddSyntheticInvoicesBeyondLimitAsync(
                    MaxRows,
                    ct);

        Assert.True(insertedRows > 0);

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
                        subjectId: "user_43"));

            using var response =
                await _environment.Client.SendAsync(
                    request,
                    ct);

            await ResponseAssertions.AssertErrorAsync(
                response,
                HttpStatusCode.BadRequest,
                "result_limit_exceeded",
                ct);
        }
        finally
        {
            await databaseMutator
                .RemoveSyntheticInvoicesAsync(ct);
        }
    }
}
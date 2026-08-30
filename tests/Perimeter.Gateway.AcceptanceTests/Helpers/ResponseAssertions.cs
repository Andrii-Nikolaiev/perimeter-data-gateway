using System.Net;
using System.Net.Http.Json;
using Perimeter.Gateway.Api.Contracts;

namespace Perimeter.Gateway.AcceptanceTests.Helpers;

public static class ResponseAssertions
{
    public static async Task<ErrorResponse> AssertErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCategory,
        CancellationToken ct)
    {
        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>(
                    cancellationToken: ct);

        Assert.NotNull(body);

        Assert.Equal(
            (int)expectedStatus,
            body.Status);

        Assert.Equal(
            expectedCategory,
            body.Category);

        return body;
    }

    public static async Task<SalesSummaryResponse>
        AssertSuccessAsync(
            HttpResponseMessage response,
            CancellationToken ct)
    {
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<SalesSummaryResponse>(
                    cancellationToken: ct);

        Assert.NotNull(body);

        Assert.Equal(
            (int)HttpStatusCode.OK,
            body.Status);

        Assert.Equal(
            "success",
            body.Category);

        Assert.Equal(
            body.Data.Count,
            body.Meta.RowsReturned);

        Assert.True(
            body.Meta.Limit > 0);

        return body;
    }
}

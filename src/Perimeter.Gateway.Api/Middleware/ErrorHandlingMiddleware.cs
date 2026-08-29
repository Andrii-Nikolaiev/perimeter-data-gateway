using System.Data.Common;
using Perimeter.Gateway.Api.Contracts;
using Perimeter.Gateway.Application.Errors;

namespace Perimeter.Gateway.Api.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (PdgException ex)
        {
            var statusCode =
                GetStatusCode(ex.Category);

            var publicCategory =
                statusCode == StatusCodes.Status500InternalServerError &&
                ex.Category != PdgErrorCategory.InternalError
                    ? PdgErrorCategory.InternalError
                    : ex.Category;

            await WriteErrorAsync(
                context,
                statusCode,
                publicCategory,
                ex);
        }
        catch (DbException ex)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                PdgErrorCategory.PlatformStoreUnavailable,
                ex);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                PdgErrorCategory.InternalError,
                ex);
        }
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string category,
        Exception exception)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        _logger.LogError(
            "Request failed. Category={Category}, ExceptionType={ExceptionType}, TraceIdentifier={TraceIdentifier}",
            category,
            exception.GetType().FullName,
            context.TraceIdentifier);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            new ErrorResponse(
                statusCode,
                category));
    }

    private static int GetStatusCode(string category)
    {
        return category switch
        {
            PdgErrorCategory.InvalidRequest =>
                StatusCodes.Status400BadRequest,

            PdgErrorCategory.ResultLimitExceeded =>
                StatusCodes.Status400BadRequest,

            PdgErrorCategory.AuthenticationFailed =>
                StatusCodes.Status401Unauthorized,

            PdgErrorCategory.AccessDenied =>
                StatusCodes.Status403Forbidden,

            PdgErrorCategory.ResourceNotFound =>
                StatusCodes.Status404NotFound,

            PdgErrorCategory.InternalError =>
                StatusCodes.Status500InternalServerError,

            PdgErrorCategory.CorporateDataSourceUnavailable =>
                StatusCodes.Status503ServiceUnavailable,

            PdgErrorCategory.PlatformStoreUnavailable =>
                StatusCodes.Status503ServiceUnavailable,

            PdgErrorCategory.AuditWriteFailed =>
                StatusCodes.Status503ServiceUnavailable,

            _ =>
                StatusCodes.Status500InternalServerError
        };
    }
}
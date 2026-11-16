using eCommerce.Domain.Persistence;
using eCommerce.ServiceInterfaces;
using Microsoft.AspNetCore.Diagnostics;
namespace eCommerce.Backend;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ServiceException e => (e.Error switch
            {
                ServiceError.NotFound => 404, ServiceError.InvalidInput => 400,
                ServiceError.CurrencyMismatch => 409, ServiceError.CartLimitExceeded => 422,
                ServiceError.VersionConflict => 412, _ => 500
            }, e.Message),
            StorageUnavailableException => (503, "Database is busy. Read the cart before retrying."),
            _ => (500, "An unexpected error occurred.")
        };
        if (status >= 500) logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
        if (status == 503) context.Response.Headers.RetryAfter = "1";
        await Results.Problem(statusCode: status, title: title,
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return true;
    }
}


using System.Diagnostics;
using eCommerce.Domain.Entities;
using Microsoft.AspNetCore.Diagnostics;

namespace eCommerce.Backend.Tracing;

internal sealed class TraceLogMiddleware(RequestDelegate next, TraceLogQueue queue)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var startedAt = DateTimeOffset.UtcNow;
        var start = Stopwatch.GetTimestamp();
        var method = context.Request.Method;
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

        // Exception handling clears response headers before writing ProblemDetails.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
            return Task.CompletedTask;
        });

        await next(context);

        var entry = new TraceLog
        {
            TraceId = Limit(traceId, 128),
            Method = Limit(method, 32),
            RouteTemplate = route is null ? null : Limit(route, 256),
            StatusCode = context.Response.StatusCode,
            StartedAt = startedAt,
            DurationMs = (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds
        };

        if (entry.StatusCode is >= 500 and < 600)
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var type = exception?.GetType().FullName;
            entry.TraceLogError = new TraceLogError { ExceptionType = type is null ? null : Limit(type, 256) };
        }

        queue.Enqueue(entry);
    }

    private static string Limit(string value, int length) => value[..Math.Min(value.Length, length)];
}

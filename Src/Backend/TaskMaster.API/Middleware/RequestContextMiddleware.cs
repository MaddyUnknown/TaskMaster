using System.Diagnostics;
using Serilog.Context;

namespace TaskMaster.API.Middleware;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerId)
            ? headerId.ToString()
            : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);

            stopwatch.Stop();
            context.Items["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}

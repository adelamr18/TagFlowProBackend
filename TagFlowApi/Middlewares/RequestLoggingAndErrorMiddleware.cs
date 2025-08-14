using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TagFlowApi.Middlewares;

public class RequestLoggingAndErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingAndErrorMiddleware> _logger;

    public RequestLoggingAndErrorMiddleware(RequestDelegate next, ILogger<RequestLoggingAndErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var qsApiKey = context.Request.Query["apiKey"].ToString();
        if (string.IsNullOrEmpty(qsApiKey))
            qsApiKey = context.Request.Query["access_token"].ToString();
        if (!string.IsNullOrEmpty(qsApiKey) && !context.Request.Headers.ContainsKey("X-Api-Key"))
            context.Request.Headers["X-Api-Key"] = qsApiKey;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            sw.Stop();

            var status = context.Response.StatusCode;

            if (status >= 500)
            {
                _logger.LogError("req_end {method} {path} {status} {elapsed_ms} trace={traceId}",
                    context.Request.Method, context.Request.Path.Value, status, sw.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
            else if (status >= 400)
            {
                _logger.LogWarning("req_end {method} {path} {status} {elapsed_ms} trace={traceId}",
                    context.Request.Method, context.Request.Path.Value, status, sw.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "unhandled_exception {method} {path} {elapsed_ms} trace={traceId}",
                context.Request.Method, context.Request.Path.Value, sw.ElapsedMilliseconds, context.TraceIdentifier);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var problem = new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}

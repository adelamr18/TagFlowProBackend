using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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
        var sw = Stopwatch.StartNew();


        try
        {
            await _next(context);
            sw.Stop();

            var status = context.Response.StatusCode;

            if (status >= 500)
            {
                _logger.LogError("req_end {method} {path} {status} {elapsed_ms}",
                    context.Request.Method, context.Request.Path.Value, status, sw.ElapsedMilliseconds);
            }
            else if (status >= 400)
            {
                _logger.LogWarning("req_end {method} {path} {status} {elapsed_ms}",
                    context.Request.Method, context.Request.Path.Value, status, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("req_end {method} {path} {status} {elapsed_ms} ",
                    context.Request.Method, context.Request.Path.Value, status, sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex, "unhandled_exception {method} {path} {elapsed_ms}",
                context.Request.Method, context.Request.Path.Value, sw.ElapsedMilliseconds);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var problem = new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = context.Request.Path,
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}

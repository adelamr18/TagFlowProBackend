using System.Diagnostics;

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

            var root = ex;
            while (root.InnerException != null) root = root.InnerException;

            var st = new StackTrace(root, true);
            var frame = st.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0) ?? st.GetFrame(0);
            var file = frame?.GetFileName() ?? "n/a";
            var line = frame?.GetFileLineNumber() ?? 0;
            var member = frame?.GetMethod()?.Name ?? "n/a";

            _logger.LogError(ex,
                "unhandled_exception {method} {path} {elapsed_ms} trace={traceId} file={file} line={line} member={member}",
                context.Request.Method,
                context.Request.Path.Value,
                sw.ElapsedMilliseconds,
                context.TraceIdentifier,
                file,
                line,
                member);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }
}

using Microsoft.Extensions.Primitives;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        var configuredApiKey =
            Environment.GetEnvironmentVariable("API_KEY") ??
            Environment.GetEnvironmentVariable("ApiKey");

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Server API key not configured.");
            return;
        }

        StringValues incoming = context.Request.Headers[ApiKeyHeaderName];
        if (StringValues.IsNullOrEmpty(incoming))
            incoming = context.Request.Query["apiKey"];
        if (StringValues.IsNullOrEmpty(incoming))
            incoming = context.Request.Query["access_token"];

        if (StringValues.IsNullOrEmpty(incoming))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key is missing.");
            return;
        }

        if (!string.Equals(incoming.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        await _next(context);
    }
}

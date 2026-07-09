using System.Diagnostics;
using System.Text.Json;
using VoucherSystem.Application;

namespace VoucherSystem.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var userId = context.Items["UserId"]?.ToString() ?? "anon";

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms [user:{UserId}]",
                method, path, statusCode, sw.ElapsedMilliseconds, userId);
        }
    }
}

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (CampaignStateMachineException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/problem+json";
            await WriteProblemDetails(context, 400, "Invalid state transition", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/problem+json";
            await WriteProblemDetails(context, 500, "An unexpected error occurred", ex.Message);
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        var problem = new
        {
            type = statusCode == 400
                ? "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                : "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title,
            status = statusCode,
            detail,
            requestId
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

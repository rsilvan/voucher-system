using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace VoucherSystem.Api.Middleware;

/// <summary>
/// Rate limits login attempts per IP address.
/// Config: max 5 attempts per minute per IP.
/// Returns 429 Too Many Requests with Retry-After header when exceeded.
/// Only applies to POST /api/auth/login.
/// </summary>
public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginRateLimitMiddleware> _logger;

    private const int MaxAttempts = 5;
    private static readonly TimeSpan WindowPeriod = TimeSpan.FromMinutes(1);

    public LoginRateLimitMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<LoginRateLimitMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to POST /api/auth/login
        if (HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            var ip = GetClientIp(context);
            var cacheKey = $"login_rate:{ip}";

            var attempts = await _cache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = WindowPeriod;
                return Task.FromResult(0);
            });

            if (attempts >= MaxAttempts)
            {
                var retryAfter = (int)WindowPeriod.TotalSeconds;
                _logger.LogWarning("Login rate limit exceeded for IP {Ip}", ip);

                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = retryAfter.ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many login attempts. Please try again later.",
                    retryAfterSeconds = retryAfter
                });
                return;
            }

            // Increment attempt counter after the request is processed
            // We use a wrapper approach: track via response
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                // Only count failed attempts (non-200 responses)
                if (context.Response.StatusCode != 200)
                {
                    _cache.Set(cacheKey, attempts + 1, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = WindowPeriod
                    });
                }
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }

            return;
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for X-Forwarded-For header first (proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

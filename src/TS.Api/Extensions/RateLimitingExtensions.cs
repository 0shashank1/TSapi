using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TS.Api.Extensions;

namespace TS.Api.Extensions;

public static class RateLimitingExtensions
{
    private const int AuthPermits = 10;
    private const int UnlockPermits = 5;
    private const int SharePermits = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                await ApiProblems.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "rate-limited",
                    "Too many requests",
                    "The request rate limit has been exceeded. Try again later.");
            };

            // POST /api/v1/auth/{register,login,refresh}
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthPermits,
                        Window = Window,
                        QueueLimit = 0
                    }));

            // POST /s/{code}/unlock: password-guessing surface, per IP + link.
            options.AddPolicy("unlock", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    $"{context.Connection.RemoteIpAddress}|{context.Request.Path}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = UnlockPermits,
                        Window = Window,
                        QueueLimit = 0
                    }));

            // GET /s/{code}
            options.AddPolicy("share", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = SharePermits,
                        Window = Window,
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = Window,
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}

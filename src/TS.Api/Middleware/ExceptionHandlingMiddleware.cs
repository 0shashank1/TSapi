using TS.Api.Extensions;
using TS.Application.Common;

namespace TS.Api.Middleware;

/// <summary>
/// Maps application exceptions to RFC 9457 Problem Details responses
/// instead of leaking stack traces as 500s.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ApiException ex) when (ex is not ConcurrencyConflictException)
        {
            await WriteApiExceptionAsync(context, ex);
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogInformation(
                "Concurrency conflict on {Path}: {Detail}",
                context.Request.Path,
                ex.Message);

            await ApiProblems.WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                "concurrency-conflict",
                "Resource was modified",
                ex.Message);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogInformation(
                "EF concurrency conflict on {Path}: {Message}",
                context.Request.Path,
                ex.Message);

            await ApiProblems.WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                "concurrency-conflict",
                "Resource was modified",
                "The resource was changed by another request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

            await ApiProblems.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal-error",
                "An unexpected error occurred",
                "An error occurred while processing the request.");
        }
    }

    private static Task WriteApiExceptionAsync(HttpContext context, ApiException ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access denied"),
            UnauthorizedException u when u.ProblemType == "password-required" =>
                (StatusCodes.Status401Unauthorized, "Password required"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        return ApiProblems.WriteAsync(
            context,
            status,
            ex.ProblemType,
            title,
            ex.Message,
            (ex as ValidationException)?.Errors);
    }
}

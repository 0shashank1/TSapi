using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TS.Api.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection AddApiControllers(
        this IServiceCollection services)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error =>
                                string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "The value is invalid."
                                    : error.ErrorMessage)
                            .ToArray());

                var problem = new Dictionary<string, object?>
                {
                    ["type"] = $"{ApiProblems.BaseUri}/validation-error",
                    ["title"] = "Validation failed",
                    ["status"] = StatusCodes.Status400BadRequest,
                    ["detail"] = "One or more validation errors occurred.",
                    ["instance"] = context.HttpContext.Request.Path.Value,
                    ["traceId"] = Activity.Current?.Id
                                 ?? context.HttpContext.TraceIdentifier,
                    ["errors"] = errors
                };

                return new ObjectResult(problem)
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }
}

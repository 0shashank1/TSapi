using System.Text.Json;

namespace TS.Api.Extensions;

/// <summary>
/// RFC 9457 Problem Details writer used by every error path.
/// </summary>
public static class ApiProblems
{
    public const string BaseUri = "https://api.textshare.dev/problems";

    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string problemType,
        string title,
        string? detail = null,
        IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"{BaseUri}/{problemType}",
            ["title"] = title,
            ["status"] = statusCode
        };

        if (detail is not null)
            payload["detail"] = detail;

        payload["instance"] = context.Request.Path.Value;
        payload["traceId"] =
            System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

        if (errors is { Count: > 0 })
            payload["errors"] = errors;

        return context.Response.WriteAsJsonAsync(
            payload,
            JsonSerializerOptions.Web,
            "application/problem+json",
            context.RequestAborted);
    }
}

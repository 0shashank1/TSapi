using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TS.Api.Services;

public static class HealthChecks
{
    /// <summary>
    /// JSON shape from the API design doc:
    /// { "status": "Healthy", "checks": { "postgresql": "Healthy" } }
    /// </summary>
    public static Task WriteResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new Dictionary<string, object?>
        {
            ["status"] = report.Status.ToString()
        };

        if (report.Entries.Count > 0)
        {
            payload["checks"] = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString());
        }

        return context.Response.WriteAsJsonAsync(
            payload,
            JsonSerializerOptions.Web);
    }
}

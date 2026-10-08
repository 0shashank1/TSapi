using Microsoft.Extensions.Diagnostics.HealthChecks;
using TS.Infrastructure.Persistence;

namespace TS.Api.Services;

/// <summary>
/// Readiness check: verifies the application can reach PostgreSQL.
/// Tagged "ready" so it only participates in /health/ready, not liveness.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly TSDbContext _dbContext;

    public DatabaseHealthCheck(TSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL health check failed.", ex);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RevolaAgent.Infrastructure.Persistence;

public sealed class PostgresHealthCheck(RevolaDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connected = await database.Database.CanConnectAsync(cancellationToken);
        return connected ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RevolaAgent.Infrastructure.Persistence;

public sealed class PostgresHealthCheck(RevolaDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connected = await database.Database.CanConnectAsync(cancellationToken);
        if (!connected) return HealthCheckResult.Unhealthy();
        var pending = await database.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending.Any() ? HealthCheckResult.Unhealthy() : HealthCheckResult.Healthy();
    }
}

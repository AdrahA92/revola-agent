using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RevolaAgent.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace RevolaAgent.IntegrationTests;

public class PostgresTests
{
    [Fact]
    [Trait("Category", "Docker")]
    public async Task FreshPostgresAcceptsDbContextAndReadinessProbe()
    {
        await using var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<RevolaDbContext>().UseNpgsql(container.GetConnectionString()).Options;
        await using var context = new RevolaDbContext(options);
        var result = await new PostgresHealthCheck(context).CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        // Phase 1 deliberately has no domain schema or migrations.
        Assert.Empty(context.Database.GetMigrations());
    }
}

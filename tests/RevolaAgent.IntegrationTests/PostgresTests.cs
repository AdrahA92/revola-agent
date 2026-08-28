using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RevolaAgent.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
using System.Net;
using System.Net.Http.Json;
using Npgsql;
using RevolaAgent.Application.Tenancy;
using static RevolaAgent.IntegrationTests.IdentityTestFactory;

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
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();
        result = await new PostgresHealthCheck(context).CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await using var factory = new IdentityTestFactory(container.GetConnectionString());
        var (aliceClient, _) = await factory.RegisterAsync("pg-alice@example.test");
        var (bobClient, _) = await factory.RegisterAsync("pg-bob@example.test");
        using var alice = aliceClient;
        using var bob = bobClient;
        var tenant = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Postgres Company" })).StatusCode);
        Assert.Empty((await bob.GetFromJsonAsync<TenantView[]>("/api/tenants"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tenants/{tenant}/audit")).StatusCode);
        // Raw SQL bypasses EF guards: the database trigger must still prevent tampering.
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditEvents\""));
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("UPDATE \"AuditEvents\" SET \"Action\" = 'tampered'"));
        Assert.Single((await alice.GetFromJsonAsync<AuditView[]>($"/api/tenants/{tenant}/audit"))!);
        var profile = new RevolaAgent.Application.Company.SaveRecord<RevolaAgent.Application.Company.CompanyProfileData>(
            Guid.Empty, Guid.NewGuid(), CompanyTests.Profile, "Owner-provided", null);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}/company/profile", profile)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tenants/{tenant}/company/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}/demo-audits/{Guid.NewGuid()}", new { scenario = "starter" })).StatusCode);
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("DELETE FROM \"CompanyRevisions\""));
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("UPDATE \"AuditRuns\" SET \"Scenario\" = 'tampered'"));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}/agent-runs/{Guid.NewGuid()}", new { goal = "Vorstellung", platform = "demo-facebook" })).StatusCode);
        var content = new RevolaAgent.Application.Content.SaveContent(Guid.Empty, Guid.NewGuid(),
            new("Vorstellung", "Unser Unternehmen", "Büro", "Ein Büro", "demo-facebook", DateTime.UtcNow.AddDays(1), "UTC"));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}/content/{Guid.NewGuid()}", content)).StatusCode);
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("DELETE FROM \"ContentVersions\""));
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync("DELETE FROM \"ContentDecisions\""));
    }
}

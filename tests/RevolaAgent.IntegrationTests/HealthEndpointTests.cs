using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace RevolaAgent.IntegrationTests;

public class HealthEndpointTests
{
    [Theory]
    [InlineData(true, HttpStatusCode.OK, "Healthy")]
    [InlineData(false, HttpStatusCode.ServiceUnavailable, "Unhealthy")]
    public async Task ReadinessReflectsDependencyWithoutLeakingDetails(bool healthy, HttpStatusCode code, string status)
    {
        await using var factory = new HealthFactory(healthy);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        Assert.Equal(code, response.StatusCode);
        Assert.Equal("{\"status\":\"" + status + "\"}", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task LivenessRemainsHealthyWhenDatabaseIsUnavailable()
    {
        await using var factory = new HealthFactory(false);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    private sealed class HealthFactory(bool healthy) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=test");
            builder.ConfigureServices(services => services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration("database", new FakeCheck(healthy), null, ["ready"]));
            }));
        }
    }

    private sealed class FakeCheck(bool healthy) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("SECRET-MUST-NOT-LEAK"));
    }
}

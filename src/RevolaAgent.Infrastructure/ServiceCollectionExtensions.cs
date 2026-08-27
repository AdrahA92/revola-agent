using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RevolaAgent.Infrastructure.Persistence;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace RevolaAgent.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddFoundation(this IHostApplicationBuilder builder, string serviceName)
    {
        var connection = DatabaseConfiguration.RequireConnectionString(
            builder.Configuration.GetConnectionString("Database"));
        builder.Services.AddDbContext<RevolaDbContext>(options => options.UseNpgsql(connection));
        builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>(
            "database", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));
        builder.Services.AddSerilog((_, logger) => logger
            .MinimumLevel.Information()
            // EF/provider exceptions can contain connection details. Never log SQL or payloads.
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.Extensions.Diagnostics.HealthChecks", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter()));
        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation().AddRuntimeInstrumentation());
        // No external telemetry traffic unless an operator explicitly sets this endpoint.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.WithTracing(tracing => tracing.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter());
        }
    }
}

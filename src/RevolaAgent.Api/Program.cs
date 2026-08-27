using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RevolaAgent.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddFoundation("RevolaAgent.Api");
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString());
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Correlation-ID"] = Activity.Current?.TraceId.ToString()
        ?? context.TraceIdentifier;
    await next(context);
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponse.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponse.WriteAsync
});
app.Run();

public partial class Program;

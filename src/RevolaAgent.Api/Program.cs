using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RevolaAgent.Infrastructure;
using RevolaAgent.Api.Identity;
using RevolaAgent.Api.Tenancy;
using RevolaAgent.Api.Company;
using RevolaAgent.Application.Company;
using RevolaAgent.Infrastructure.Company;
using RevolaAgent.Application.Audits;
using RevolaAgent.Infrastructure.Audits;

var builder = WebApplication.CreateBuilder(args);
builder.AddFoundation("RevolaAgent.Api");
builder.AddIdentityFoundation();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IAuditService, DemoAuditService>();
builder.Services.AddSingleton<IDemoPlatform, DemoPlatform>();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
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
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.Use(SecurityMiddleware.Invoke);
app.MapIdentityFoundation();
app.MapTenancy();
app.MapCompany();
app.MapDemoAudits();
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

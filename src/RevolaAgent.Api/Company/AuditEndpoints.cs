using System.Security.Claims;
using RevolaAgent.Api.Identity;
using RevolaAgent.Application.Audits;

namespace RevolaAgent.Api.Company;

public static class AuditEndpoints
{
    public static void MapDemoAudits(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants/{tenantId:guid}/demo-audits").RequireAuthorization().RequireRateLimiting("tenancy");
        group.MapGet("/", async (Guid tenantId, int? page, ClaimsPrincipal user, IAuditService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(IdentityEndpoints.UserId(user), tenantId, page ?? 1, ct)));
        group.MapPut("/{id:guid}", async (Guid tenantId, Guid id, RunRequest request, ClaimsPrincipal user, IAuditService service, CancellationToken ct) =>
            Results.Ok(await service.RunAsync(IdentityEndpoints.UserId(user), tenantId, id, request.Scenario, ct)));
    }
    public sealed record RunRequest(string Scenario);
}

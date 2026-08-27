using System.Security.Claims;
using RevolaAgent.Api.Identity;
using RevolaAgent.Application.Company;

namespace RevolaAgent.Api.Company;

public static class CompanyEndpoints
{
    public static void MapCompany(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants/{tenantId:guid}/company").RequireAuthorization().RequireRateLimiting("tenancy");
        group.MapGet("/profile", async (Guid tenantId, ClaimsPrincipal user, ICompanyService service, CancellationToken ct) =>
            Results.Ok(new { profile = await service.ProfileAsync(IdentityEndpoints.UserId(user), tenantId, ct) }));
        group.MapPut("/profile", async (Guid tenantId, SaveRecord<CompanyProfileData> request, ClaimsPrincipal user, ICompanyService service, CancellationToken ct) =>
            Results.Ok(await service.SaveProfileAsync(IdentityEndpoints.UserId(user), tenantId, request, ct)));
        group.MapGet("/knowledge", async (Guid tenantId, int? page, ClaimsPrincipal user, ICompanyService service, CancellationToken ct) =>
            Results.Ok(await service.KnowledgeAsync(IdentityEndpoints.UserId(user), tenantId, page ?? 1, ct)));
        group.MapPut("/knowledge/{id:guid}", async (Guid tenantId, Guid id, SaveRecord<KnowledgeData> request, ClaimsPrincipal user, ICompanyService service, CancellationToken ct) =>
            Results.Ok(await service.SaveKnowledgeAsync(IdentityEndpoints.UserId(user), tenantId, id, request, ct)));
        group.MapGet("/history/{id:guid}", async (Guid tenantId, Guid id, int? page, ClaimsPrincipal user, ICompanyService service, CancellationToken ct) =>
            Results.Ok(await service.HistoryAsync(IdentityEndpoints.UserId(user), tenantId, id, page ?? 1, ct)));
    }
}

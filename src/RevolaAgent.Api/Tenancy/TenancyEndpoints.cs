using System.Security.Claims;
using RevolaAgent.Api.Identity;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Tenancy;

namespace RevolaAgent.Api.Tenancy;

public static class TenancyEndpoints
{
    public static void MapTenancy(this WebApplication app)
    {
        var group = app.MapGroup("/api").RequireAuthorization().RequireRateLimiting("tenancy");
        group.MapGet("/tenants", async (ClaimsPrincipal user, ITenancyService service, CancellationToken ct, int page = 1) =>
            Results.Ok(await service.ListAsync(IdentityEndpoints.UserId(user), page, ct)));
        group.MapGet("/tenants/{tenantId:guid}", async (Guid tenantId, ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(IdentityEndpoints.UserId(user), tenantId, ct)));
        group.MapPut("/tenants/{tenantId:guid}", async (Guid tenantId, CreateTenant request,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
            Results.Ok(await service.CreateAsync(IdentityEndpoints.UserId(user), tenantId, request.Name ?? "", ct)));
        group.MapGet("/tenants/{tenantId:guid}/members", async (Guid tenantId,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct, int page = 1) =>
            Results.Ok(await service.MembersAsync(IdentityEndpoints.UserId(user), tenantId, page, ct)));
        group.MapPut("/tenants/{tenantId:guid}/members/{memberId:guid}/invitation", async (Guid tenantId, Guid memberId, InviteMember request,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
            Results.Ok(await service.InviteAsync(IdentityEndpoints.UserId(user), tenantId, memberId, ParseRole(request.Role), ct)));
        group.MapGet("/invitations", async (ClaimsPrincipal user, ITenancyService service, CancellationToken ct, int page = 1) =>
            Results.Ok(await service.InvitationsAsync(IdentityEndpoints.UserId(user), page, ct)));
        group.MapPut("/invitations/{tenantId:guid}/accept", async (Guid tenantId, VersionRequest request,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
        {
            await service.AcceptAsync(IdentityEndpoints.UserId(user), tenantId, request.Version, ct);
            return Results.NoContent();
        });
        group.MapPut("/tenants/{tenantId:guid}/members/{memberId:guid}/role", async (Guid tenantId, Guid memberId, ChangeRole request,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeRoleAsync(IdentityEndpoints.UserId(user), tenantId, memberId, ParseRole(request.Role), request.Version, ct)));
        group.MapDelete("/tenants/{tenantId:guid}/members/{memberId:guid}", async (Guid tenantId, Guid memberId, Guid version,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(IdentityEndpoints.UserId(user), tenantId, memberId, version, ct);
            return Results.NoContent();
        });
        group.MapGet("/tenants/{tenantId:guid}/audit", async (Guid tenantId,
            ClaimsPrincipal user, ITenancyService service, CancellationToken ct, int page = 1) =>
            Results.Ok(await service.AuditAsync(IdentityEndpoints.UserId(user), tenantId, page, ct)));
    }

    private static TenantRole ParseRole(string role) => Enum.TryParse<TenantRole>(role, false, out var parsed) &&
        Enum.IsDefined(parsed) && parsed.ToString() == role ? parsed : throw new TenancyException(400, "Ungültige Rolle.");
    public sealed record CreateTenant(string Name);
    public sealed record InviteMember(string Role);
    public sealed record ChangeRole(string Role, Guid Version);
    public sealed record VersionRequest(Guid Version);
}

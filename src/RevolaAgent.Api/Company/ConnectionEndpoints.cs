using System.Security.Claims;
using RevolaAgent.Api.Identity;
using RevolaAgent.Application.Tenancy;

namespace RevolaAgent.Api.Company;

public static class ConnectionEndpoints
{
    public static void MapConnectionCapabilities(this WebApplication app)
    {
        app.MapGet("/api/tenants/{tenantId:guid}/connections", async (Guid tenantId, ClaimsPrincipal user,
            ITenancyService tenancy, CancellationToken ct) =>
        {
            await tenancy.ResolveAsync(IdentityEndpoints.UserId(user), tenantId, ct);
            // No fabricated connected accounts: these are available workflow modes, not OAuth sessions.
            return Results.Ok(new[]
            {
                new ConnectionMode("facebook", "Facebook", false, false, false, true, "https://www.facebook.com/"),
                new ConnectionMode("linkedin", "LinkedIn", false, false, false, true, "https://www.linkedin.com/")
            });
        }).RequireAuthorization().RequireRateLimiting("tenancy");
    }
    public sealed record ConnectionMode(string Platform, string Name, bool Connected, bool CanReadAccount,
        bool CanPublish, bool ManualPreparationAvailable, string Website);
}

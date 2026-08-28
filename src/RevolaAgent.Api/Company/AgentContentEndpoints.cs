using System.Security.Claims;
using RevolaAgent.Api.Identity;
using RevolaAgent.Application.Agents;
using RevolaAgent.Application.Content;

namespace RevolaAgent.Api.Company;

public static class AgentContentEndpoints
{
    public static void MapAgentContent(this WebApplication app)
    {
        var root = app.MapGroup("/api/tenants/{tenantId:guid}").RequireAuthorization().RequireRateLimiting("tenancy");
        root.MapGet("/agent-runs", async (Guid tenantId, int? page, ClaimsPrincipal user, IAgentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(IdentityEndpoints.UserId(user), tenantId, page ?? 1, ct)));
        root.MapPut("/agent-runs/{id:guid}", async (Guid tenantId, Guid id, AgentRequest request, ClaimsPrincipal user, IAgentService service, CancellationToken ct) =>
            Results.Ok(await service.RunAsync(IdentityEndpoints.UserId(user), tenantId, id, request.Goal, request.Platform, ct)));
        root.MapGet("/content", async (Guid tenantId, int? page, ClaimsPrincipal user, IContentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(IdentityEndpoints.UserId(user), tenantId, page ?? 1, ct)));
        root.MapPut("/content/{id:guid}", async (Guid tenantId, Guid id, SaveContent request, ClaimsPrincipal user, IContentService service, CancellationToken ct) =>
            Results.Ok(await service.SaveAsync(IdentityEndpoints.UserId(user), tenantId, id, request, ct)));
        root.MapPost("/content/{id:guid}/decision", async (Guid tenantId, Guid id, DecisionRequest request, ClaimsPrincipal user, IContentService service, CancellationToken ct) =>
            Results.Ok(await service.TransitionAsync(IdentityEndpoints.UserId(user), tenantId, id, request, ct)));
        root.MapGet("/content/{id:guid}/history", async (Guid tenantId, Guid id, int? page, ClaimsPrincipal user, IContentService service, CancellationToken ct) =>
            Results.Ok(await service.HistoryAsync(IdentityEndpoints.UserId(user), tenantId, id, page ?? 1, ct)));
    }
    public sealed record AgentRequest(string Goal, string Platform);
}

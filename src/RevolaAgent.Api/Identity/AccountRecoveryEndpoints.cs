using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Api.Identity;

public static class AccountRecoveryEndpoints
{
    public static void MapAccountRecovery(this RouteGroupBuilder group)
    {
        group.MapPost("/request-confirmation", async (EmailRequest request, IWebHostEnvironment environment,
            UserManager<ApplicationUser> users, IIdentityDelivery delivery, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment()) return Results.Problem(statusCode: 503, title: "E-Mail-Versand nicht freigeschaltet.");
            if (request.Email?.Length is not (> 0 and <= 254) || !new EmailAddressAttribute().IsValid(request.Email)) return Invalid();
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is { EmailConfirmed: false })
            {
                try { await delivery.SendAsync(new(user.Email!, user.Id, "confirm", IdentitySecurity.Encode(await users.GenerateEmailConfirmationTokenAsync(user))), ct); }
                catch (System.Net.Mail.SmtpException) { /* Do not disclose account existence or delivery outcome. */ }
            }
            return Results.Json(new { accepted = true }, statusCode: 202);
        }).RequireRateLimiting("identity");
        group.MapPost("/confirm-email", async (TokenRequest request, UserManager<ApplicationUser> users,
            RevolaDbContext db, CancellationToken ct) =>
        {
            var token = IdentitySecurity.Decode(request.Token);
            var user = await users.FindByIdAsync(request.UserId.ToString());
            if (token is null || user is null || user.EmailConfirmed) return Invalid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!(await users.ConfirmEmailAsync(user, token)).Succeeded) return Invalid();
            db.AuditEvents.Add(new AuditEvent { ActorId = user.Id, Action = "identity.email_confirmed" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireRateLimiting("identity");

        group.MapPost("/request-reset", async (EmailRequest request, IWebHostEnvironment environment,
            UserManager<ApplicationUser> users, IIdentityDelivery delivery, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment()) return Results.Problem(statusCode: 503, title: "E-Mail-Versand nicht freigeschaltet.");
            if (request.Email?.Length is not (> 0 and <= 254) || !new EmailAddressAttribute().IsValid(request.Email)) return Invalid();
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is { EmailConfirmed: true })
            {
                try
                {
                    await delivery.SendAsync(new(user.Email!, user.Id, "reset",
                        IdentitySecurity.Encode(await users.GeneratePasswordResetTokenAsync(user))), ct);
                }
                catch (System.Net.Mail.SmtpException) { /* Same response for known and unknown accounts. */ }
            }
            return Results.Json(new { accepted = true }, statusCode: 202);
        }).RequireRateLimiting("identity");

        group.MapPost("/reset-password", async (ResetRequest request, UserManager<ApplicationUser> users,
            RevolaDbContext db, CancellationToken ct) =>
        {
            var token = IdentitySecurity.Decode(request.Token);
            if (token is null || request.NewPassword?.Length is not (>= 12 and <= 128)) return Invalid();
            var user = await users.FindByIdAsync(request.UserId.ToString());
            if (user is not { EmailConfirmed: true }) return Invalid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!(await users.ResetPasswordAsync(user, token, request.NewPassword)).Succeeded) return Invalid();
            await IdentitySecurity.RevokeSessions(db, user.Id, ct);
            db.AuditEvents.Add(new AuditEvent { ActorId = user.Id, Action = "identity.password_reset" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireRateLimiting("identity");

        group.MapGet("/sessions", async (ClaimsPrincipal principal, HttpContext context, RevolaDbContext db,
            int? page, CancellationToken ct) =>
        {
            var index = page ?? 1;
            if (index is < 1 or > 10000) return Invalid();
            var userId = IdentityEndpoints.UserId(principal);
            var authentication = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            string? sessionId = null;
            authentication.Properties?.Items.TryGetValue("revola.session", out sessionId);
            Guid.TryParse(sessionId, out var current);
            var sessions = await db.LoginSessions.AsNoTracking()
                .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((index - 1) * 50).Take(50)
                .Select(x => new { x.Id, x.CreatedAt, x.ExpiresAt, IsCurrent = x.Id == current }).ToListAsync(ct);
            return Results.Ok(sessions);
        }).RequireAuthorization();

        group.MapDelete("/sessions/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            RevolaDbContext db, CancellationToken ct) =>
        {
            var userId = IdentityEndpoints.UserId(principal);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var count = await db.LoginSessions.Where(x => x.Id == id && x.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
            if (count == 0) return Results.NotFound();
            db.AuditEvents.Add(new AuditEvent { ActorId = userId, Action = "identity.session_revoked", SubjectId = id });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("identity");
    }

    private static IResult Invalid() => Results.Problem(statusCode: 400, title: "Kontodaten konnten nicht verarbeitet werden.");
    public sealed record TokenRequest(Guid UserId, string Token);
    public sealed record EmailRequest(string Email);
    public sealed record ResetRequest(Guid UserId, string Token, string NewPassword);
}

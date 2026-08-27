using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Api.Identity;

public static class MfaEndpoints
{
    public static void MapMfa(this RouteGroupBuilder identity)
    {
        var group = identity.MapGroup("/mfa").RequireAuthorization().RequireRateLimiting("identity");
        group.MapGet("/status", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null ? Results.Unauthorized() : Results.Ok(new { user.TwoFactorEnabled,
                recoveryCodesRemaining = await users.CountRecoveryCodesAsync(user) });
        });
        group.MapPost("/setup", async (Proof request, ClaimsPrincipal principal, UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || !await PasswordValid(request, user, signIn)) return Results.Unauthorized();
            if (user.TwoFactorEnabled) return Results.Conflict();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (key is null)
            {
                if (!(await users.ResetAuthenticatorKeyAsync(user)).Succeeded) return Results.Conflict();
                key = await users.GetAuthenticatorKeyAsync(user);
            }
            db.AuditEvents.Add(new AuditEvent { ActorId = user.Id, Action = "identity.mfa_setup" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await signIn.RefreshSignInAsync(user);
            return Results.Ok(new { sharedKey = key });
        });
        group.MapPost("/enable", async (Proof request, ClaimsPrincipal principal, UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || !await PasswordValid(request, user, signIn)) return Results.Unauthorized();
            if (user.TwoFactorEnabled) return Results.Conflict();
            // Setup only accepts an authenticator code, never a pre-existing recovery code.
            if (!await IdentitySecurity.VerifySecondFactor(users, user, request.Code, null))
            {
                await users.AccessFailedAsync(user);
                return Results.Unauthorized();
            }
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!(await users.SetTwoFactorEnabledAsync(user, true)).Succeeded) return Results.Conflict();
            var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            if (codes is null || !(await users.UpdateSecurityStampAsync(user)).Succeeded) return Results.Conflict();
            await IdentitySecurity.RevokeSessions(db, user.Id, ct);
            db.AuditEvents.Add(new AuditEvent { ActorId = user.Id, Action = "identity.mfa_enabled" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await signIn.SignOutAsync();
            return Results.Ok(new { recoveryCodes = codes.ToArray() });
        });
        group.MapPost("/disable", async (Proof request, ClaimsPrincipal principal, UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || !await PasswordValid(request, user, signIn)) return Results.Unauthorized();
            if (!user.TwoFactorEnabled) return Results.Conflict();
            if (!await IdentitySecurity.VerifySecondFactor(users, user, request.Code, request.RecoveryCode))
            {
                await users.AccessFailedAsync(user);
                return Results.Unauthorized();
            }
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!(await users.SetTwoFactorEnabledAsync(user, false)).Succeeded ||
                !(await users.ResetAuthenticatorKeyAsync(user)).Succeeded ||
                await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 0) is null ||
                !(await users.UpdateSecurityStampAsync(user)).Succeeded) return Results.Conflict();
            await IdentitySecurity.RevokeSessions(db, user.Id, ct);
            db.AuditEvents.Add(new AuditEvent { ActorId = user.Id, Action = "identity.mfa_disabled" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await signIn.SignOutAsync();
            return Results.NoContent();
        });
    }

    private static async Task<bool> PasswordValid(Proof request, ApplicationUser user, SignInManager<ApplicationUser> signIn) =>
        request.Password?.Length is > 0 and <= 128 &&
        (await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded;
    public sealed record Proof(string Password, string? Code = null, string? RecoveryCode = null);
}

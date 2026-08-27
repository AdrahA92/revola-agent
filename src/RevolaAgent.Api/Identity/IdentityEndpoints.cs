using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Api.Identity;

public static class IdentityEndpoints
{
    public static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static void MapIdentityFoundation(this WebApplication app)
    {
        var group = app.MapGroup("/api/identity");
        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
        group.MapPost("/register", Register).RequireRateLimiting("identity");
        group.MapPost("/login", Login).RequireRateLimiting("identity");
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new { id = UserId(user) })).RequireAuthorization();
        group.MapPost("/logout", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!(await users.UpdateSecurityStampAsync(user)).Succeeded) return Results.Conflict();
            Audit(db, user.Id, "identity.sessions_revoked");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("identity");
        group.MapPost("/password", async (PasswordRequest request, ClaimsPrincipal principal,
            UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct) =>
        {
            if (request.CurrentPassword?.Length is not (>= 1 and <= 128) || request.NewPassword?.Length is not (>= 12 and <= 128))
                return InvalidCredentials();
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded) return InvalidCredentials();
            Audit(db, user.Id, "identity.password_changed");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("identity");
    }

    private static async Task<IResult> Register(Credentials request, IWebHostEnvironment environment,
        UserManager<ApplicationUser> users, RevolaDbContext db, CancellationToken ct)
    {
        // Production onboarding stays closed until email verification/delivery is configured and tested.
        if (!environment.IsDevelopment()) return Results.Problem(statusCode: 503, title: "Registrierung noch nicht freigeschaltet.");
        if (!Valid(request) || request.Password.Length < 12) return InvalidCredentials();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = request.Email.Trim(), Email = request.Email.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return InvalidCredentials();
        Audit(db, user.Id, "identity.registered");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    private static async Task<IResult> Login(Credentials request, UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn, RevolaDbContext db, CancellationToken ct)
    {
        if (!Valid(request)) return Results.Unauthorized();
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            // Keep an expensive password operation on the unknown-user path as well.
            users.PasswordHasher.HashPassword(new ApplicationUser(), request.Password);
            return Results.Unauthorized();
        }
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        // No accidental bypass if MFA is enabled by an operator before the MFA flow is implemented.
        if (!result.Succeeded || user.TwoFactorEnabled) return Results.Unauthorized();
        Audit(db, user.Id, "identity.signed_in");
        await db.SaveChangesAsync(ct);
        var properties = new AuthenticationProperties { IsPersistent = false };
        properties.Items["revola.started"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        await signIn.SignInAsync(user, properties);
        return Results.NoContent();
    }

    private static bool Valid(Credentials request) => request.Email?.Length is > 0 and <= 254 &&
        new EmailAddressAttribute().IsValid(request.Email) && request.Password?.Length is > 0 and <= 128;
    private static IResult InvalidCredentials() => Results.Problem(statusCode: 400, title: "Kontodaten konnten nicht verarbeitet werden.");
    private static void Audit(RevolaDbContext db, Guid userId, string action) =>
        db.AuditEvents.Add(new AuditEvent { ActorId = userId, Action = action });
    public sealed record Credentials(string Email, string Password);
    public sealed record PasswordRequest(string CurrentPassword, string NewPassword);
}

using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;
using RevolaAgent.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace RevolaAgent.Api.Identity;

public static class IdentityConfiguration
{
    public static void AddIdentityFoundation(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.SignIn.RequireConfirmedEmail = true;
        }).AddEntityFrameworkStores<RevolaDbContext>().AddDefaultTokenProviders();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = builder.Environment.IsDevelopment() ? "revola.session" : "__Host-revola.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.Events.OnValidatePrincipal = async context =>
            {
                // Stamp validation can renew a cookie; keep an independent absolute session deadline.
                if (!context.Properties.Items.TryGetValue("revola.started", out var started) ||
                    !long.TryParse(started, out var seconds) || seconds > DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds() - seconds >= 1800)
                {
                    context.RejectPrincipal();
                    return;
                }
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                if (context.Principal?.Identity?.IsAuthenticated != true) return;
                if (!context.Properties.Items.TryGetValue("revola.session", out var sessionText) || !Guid.TryParse(sessionText, out var sessionId))
                {
                    context.RejectPrincipal();
                    return;
                }
                var userId = IdentityEndpoints.UserId(context.Principal);
                var db = context.HttpContext.RequestServices.GetRequiredService<RevolaDbContext>();
                if (!await db.LoginSessions.AnyAsync(x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow,
                    context.HttpContext.RequestAborted)) context.RejectPrincipal();
            };
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        // Revocation applies on the very next authenticated request, not after a cached interval.
        builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));
        builder.Services.AddScoped<IIdentityDelivery, LocalIdentityDelivery>();
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = builder.Environment.IsDevelopment() ? "revola.csrf" : "__Host-revola.csrf";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("identity", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("tenancy", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        builder.Services.AddScoped<ITenancyService, TenancyService>();
    }
}

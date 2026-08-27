using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RevolaAgent.Infrastructure.Persistence;
using Xunit;
using static RevolaAgent.IntegrationTests.IdentityTestFactory;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace RevolaAgent.IntegrationTests;

public class IdentitySecurityTests
{
    [Fact]
    public async Task ProductionRegistrationStaysClosedAndCsrfCookieIsSecure()
    {
        await using var factory = new IdentityTestFactory(environment: "Production");
        using var client = factory.NewClient();
        var csrf = await client.GetAsync("/api/identity/csrf");
        var cookie = Assert.Single(csrf.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Host-revola.csrf=", cookie);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        var result = await SendAsync(client, HttpMethod.Post, "/api/identity/register", new { email = "closed@example.test", password = Password });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task AbsoluteSessionLifetimeCannotBeExtendedByCookieRenewal()
    {
        await using var factory = new IdentityTestFactory();
        var (client, _) = await factory.RegisterAsync("expiry@example.test");
        using var original = client;
        using var loginClient = factory.NewClient();
        var login = await SendAsync(loginClient, HttpMethod.Post, "/api/identity/login", new { email = "expiry@example.test", password = Password });
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith("revola.session="));
        var encrypted = cookie.Split(';')[0]["revola.session=".Length..];
        var format = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme).TicketDataFormat;
        var ticket = format.Unprotect(encrypted)!;
        Assert.NotNull(ticket);
        // Simulate a renewed, still cryptographically valid ticket belonging to an old session.
        ticket.Properties.Items["revola.started"] = DateTimeOffset.UtcNow.AddMinutes(-31).ToUnixTimeSeconds().ToString();
        using var replay = factory.CreateClient(new WebApplicationFactoryClientOptions
        { HandleCookies = false, AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        replay.DefaultRequestHeaders.Add("Cookie", "revola.session=" + format.Protect(ticket));
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/identity/me")).StatusCode);
    }

    [Fact]
    public async Task AuthenticationEndpointsEnforceRateLimit()
    {
        await using var factory = new IdentityTestFactory();
        using var client = factory.NewClient();
        for (var attempt = 0; attempt < 20; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Post, "/api/identity/login",
                new { email = "invalid", password = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, HttpMethod.Post, "/api/identity/login",
            new { email = "invalid", password = "" })).StatusCode);
    }

    [Fact]
    public async Task RegistrationNeedsCsrfAndRejectsWeakPassword()
    {
        await using var factory = new IdentityTestFactory();
        using var client = factory.NewClient();
        var response = await client.PostAsJsonAsync("/api/identity/register", new { email = "a@example.test", password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response = await SendAsync(client, HttpMethod.Post, "/api/identity/register", new { email = "a@example.test", password = "weak" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tenants")).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<RevolaDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task LogoutRevokesOtherSessionsAndProtectedWritesNeedCsrf()
    {
        await using var factory = new IdentityTestFactory();
        var (client, _) = await factory.RegisterAsync("logout@example.test");
        using var first = client;
        using var second = factory.NewClient();
        var login = await SendAsync(second, HttpMethod.Post, "/api/identity/login", new { email = "logout@example.test", password = Password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith("revola.session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", new { name = "Blocked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(first, HttpMethod.Post, "/api/identity/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/identity/me")).StatusCode);
    }

    [Fact]
    public async Task RepeatedBadPasswordsLockAccountAndNeverLeakCredentials()
    {
        await using var factory = new IdentityTestFactory();
        var (client, _) = await factory.RegisterAsync("locked@example.test");
        using var original = client;
        using var attacker = factory.NewClient();
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(attacker, HttpMethod.Post, "/api/identity/login",
                new { email = "locked@example.test", password = "Wrong-Password-42!" })).StatusCode);
        var response = await SendAsync(attacker, HttpMethod.Post, "/api/identity/login", new { email = "locked@example.test", password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(Password, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PasswordChangeInvalidatesExistingSessions()
    {
        await using var factory = new IdentityTestFactory();
        var (client, _) = await factory.RegisterAsync("password@example.test");
        using var first = client;
        using var other = factory.NewClient();
        await SendAsync(other, HttpMethod.Post, "/api/identity/login", new { email = "password@example.test", password = Password });
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(first, HttpMethod.Post, "/api/identity/password",
            new { currentPassword = Password, newPassword = "Changed-Test-Password-42!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/identity/me")).StatusCode);
    }
}

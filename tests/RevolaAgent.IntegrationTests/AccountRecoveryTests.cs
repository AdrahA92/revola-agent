using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RevolaAgent.IntegrationTests;

public sealed class AccountRecoveryTests
{
    [Fact]
    public async Task Confirmation_is_required_bound_to_user_and_not_reusable()
    {
        await using var factory = new IdentityTestFactory();
        using var client = factory.NewClient();
        var credentials = new { email = "confirmation@example.test", password = IdentityTestFactory.Password };
        Assert.Equal(HttpStatusCode.Created, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/register", credentials)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/login", credentials)).StatusCode);
        var message = factory.Delivery.Messages.Single();
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/confirm-email", new { userId = Guid.NewGuid(), message.Token })).StatusCode);
        var proof = new { message.UserId, message.Token };
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/confirm-email", proof)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/confirm-email", proof)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/login", credentials)).StatusCode);
    }

    [Fact]
    public async Task Reset_is_one_time_and_revokes_sessions_without_enumerating_accounts()
    {
        await using var factory = new IdentityTestFactory();
        var (client, _) = await factory.RegisterAsync("reset@example.test");
        using var anonymous = factory.NewClient();
        foreach (var email in new[] { "reset@example.test", "unknown@example.test" })
            Assert.Equal(HttpStatusCode.Accepted, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/request-reset", new { email })).StatusCode);
        var message = factory.Delivery.Messages.Single(x => x.Purpose == "reset");
        var proof = new { message.UserId, message.Token, newPassword = "Replacement-Test-Password-43!" };
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/reset-password", new { userId = Guid.NewGuid(), message.Token, proof.newPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/reset-password", proof)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/reset-password", proof)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/login", new { email = "reset@example.test", password = IdentityTestFactory.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(anonymous, HttpMethod.Post, "/api/identity/login", new { email = "reset@example.test", password = proof.newPassword })).StatusCode);
    }

    [Fact]
    public async Task Sessions_are_private_and_individually_revocable()
    {
        await using var factory = new IdentityTestFactory();
        var (alice, _) = await factory.RegisterAsync("session-a@example.test");
        var (bob, _) = await factory.RegisterAsync("session-b@example.test");
        var sessions = await alice.GetFromJsonAsync<JsonElement>("/api/identity/sessions");
        var id = sessions[0].GetProperty("id").GetGuid();
        Assert.True(sessions[0].GetProperty("isCurrent").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await IdentityTestFactory.SendAsync(bob, HttpMethod.Delete, $"/api/identity/sessions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(alice, HttpMethod.Delete, $"/api/identity/sessions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await alice.GetAsync("/api/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/identity/me")).StatusCode);
    }
}

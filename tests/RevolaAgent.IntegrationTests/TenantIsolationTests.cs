using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Infrastructure.Persistence;
using Xunit;
using static RevolaAgent.IntegrationTests.IdentityTestFactory;

namespace RevolaAgent.IntegrationTests;

public class TenantIsolationTests
{
    [Fact]
    public async Task ForeignTenantCannotBeReadChangedOrReferenced()
    {
        await using var factory = new IdentityTestFactory();
        var (aliceClient, aliceId) = await factory.RegisterAsync("alice@example.test");
        var (bobClient, _) = await factory.RegisterAsync("bob@example.test");
        using var alice = aliceClient;
        using var bob = bobClient;
        var tenant = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(alice, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Alice Company" })).StatusCode);
        Assert.Empty((await bob.GetFromJsonAsync<TenantView[]>("/api/tenants"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tenants/{tenant}/members")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tenants/{tenant}/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(bob, HttpMethod.Put, $"/api/tenants/{tenant}/members/{aliceId}/role",
            new { role = "Viewer", version = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(bob, HttpMethod.Delete,
            $"/api/tenants/{tenant}/members/{aliceId}?version={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(bob, HttpMethod.Put, $"/api/invitations/{tenant}/accept",
            new { version = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(bob, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Hijack" })).StatusCode);
        Assert.Single((await alice.GetFromJsonAsync<TenantView[]>("/api/tenants"))!);
    }

    [Fact]
    public async Task InvitationsNeedConsentAndRolesAreCheckedOnEveryRequest()
    {
        await using var factory = new IdentityTestFactory();
        var (ownerClient, ownerId) = await factory.RegisterAsync("owner@example.test");
        var (memberClient, memberId) = await factory.RegisterAsync("member@example.test");
        using var owner = ownerClient;
        using var member = memberClient;
        var tenant = Guid.NewGuid();
        await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Example Company" });
        var invited = await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}/members/{memberId}/invitation", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.OK, invited.StatusCode);
        var invitation = (await invited.Content.ReadFromJsonAsync<MemberView>())!;
        Assert.False(invitation.Active);
        Assert.Empty((await member.GetFromJsonAsync<TenantView[]>("/api/tenants"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/tenants/{tenant}/members")).StatusCode);
        Assert.Single((await member.GetFromJsonAsync<InvitationView[]>("/api/invitations"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(member, HttpMethod.Put, $"/api/invitations/{tenant}/accept", new { invitation.Version })).StatusCode);
        var members = (await owner.GetFromJsonAsync<MemberView[]>($"/api/tenants/{tenant}/members"))!;
        var current = members.Single(x => x.UserId == memberId);
        var ownerRow = members.Single(x => x.UserId == ownerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(member, HttpMethod.Put, $"/api/tenants/{tenant}/members/{ownerId}/role",
            new { role = "Viewer", version = ownerRow.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(owner, HttpMethod.Delete,
            $"/api/tenants/{tenant}/members/{ownerId}?version={ownerRow.Version}")).StatusCode);
        var demotion = await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}/members/{memberId}/role", new { role = "Viewer", current.Version });
        Assert.Equal(HttpStatusCode.OK, demotion.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync($"/api/tenants/{tenant}/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync($"/api/tenants/{tenant}/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}/members/{memberId}/role",
            new { role = "Editor", current.Version })).StatusCode);
        var demoted = (await demotion.Content.ReadFromJsonAsync<MemberView>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(owner, HttpMethod.Delete,
            $"/api/tenants/{tenant}/members/{memberId}?version={demoted.Version}")).StatusCode);
        Assert.Empty((await member.GetFromJsonAsync<TenantView[]>("/api/tenants"))!);
    }

    [Fact]
    public async Task CreationIsIdempotentAndAuditCannotBeModified()
    {
        await using var factory = new IdentityTestFactory();
        var (ownerClient, _) = await factory.RegisterAsync("audit@example.test");
        using var owner = ownerClient;
        var tenant = Guid.NewGuid();
        for (var attempt = 0; attempt < 2; attempt++)
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Audit Company" })).StatusCode);
        var audit = (await owner.GetFromJsonAsync<AuditView[]>($"/api/tenants/{tenant}/audit"))!;
        Assert.Single(audit);
        Assert.Equal("tenant.created", audit[0].Action);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RevolaDbContext>();
        var entry = await db.AuditEvents.SingleAsync(x => x.TenantId == tenant);
        entry.Action = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(entry).State = EntityState.Unchanged;
        db.AuditEvents.Remove(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}

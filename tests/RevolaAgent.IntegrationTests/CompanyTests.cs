using System.Net;
using System.Net.Http.Json;
using RevolaAgent.Application.Company;
using RevolaAgent.Application.Tenancy;
using Xunit;
using static RevolaAgent.IntegrationTests.IdentityTestFactory;

namespace RevolaAgent.IntegrationTests;

public sealed class CompanyTests
{
    public static CompanyProfileData Profile => new("Example", "Software", "Example company", "company@example.test",
        "https://example.test", "Development", "Local businesses", "Example region", "#006666", "Clear", "Verified facts", "No guarantees", "More inquiries");

    [Fact]
    public async Task Profile_and_knowledge_are_isolated_versioned_validated_and_idempotent()
    {
        await using var factory = new IdentityTestFactory();
        var (owner, _) = await factory.RegisterAsync("company-owner@example.test");
        var (other, _) = await factory.RegisterAsync("company-other@example.test");
        var tenant = Guid.NewGuid();
        await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Example Company" });
        var root = $"/api/tenants/{tenant}/company";
        var initial = new SaveRecord<CompanyProfileData>(Guid.Empty, Guid.NewGuid(), Profile, "Provided by the owner", null);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + "/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(other, HttpMethod.Put, root + "/profile", initial)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Put, root + "/profile", initial with { Data = Profile with { Website = "javascript:alert(1)" } })).StatusCode);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Put, root + "/profile", initial)).StatusCode);
        var next = initial with { Version = initial.NewVersion, NewVersion = Guid.NewGuid(), Data = Profile with { Description = "Corrected by owner" } };
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Put, root + "/profile", next)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(owner, HttpMethod.Put, root + "/profile", initial with { NewVersion = Guid.NewGuid() })).StatusCode);
        var history = (await owner.GetFromJsonAsync<RevisionView[]>(root + $"/history/{tenant}"))!;
        Assert.Equal(2, history.Length);
        Assert.Contains(history, x => x.Version == initial.NewVersion && x.DataJson.Contains("Example company"));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + $"/history/{tenant}")).StatusCode);
        var id = Guid.NewGuid();
        var knowledge = new SaveRecord<KnowledgeData>(Guid.Empty, Guid.NewGuid(), new("Delivery", "Only by appointment"), "Owner instruction", DateTime.UtcNow.AddDays(30));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(owner, HttpMethod.Put, root + $"/knowledge/{id}", knowledge)).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<RecordView<KnowledgeData>[]>(root + "/knowledge"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root + "/knowledge")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(other, HttpMethod.Put, root + $"/knowledge/{id}", knowledge)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(root + $"/history/{Guid.NewGuid()}")).StatusCode);
    }

    [Theory]
    [InlineData("Viewer", false)]
    [InlineData("Editor", false)]
    [InlineData("Approver", false)]
    [InlineData("Manager", true)]
    [InlineData("Admin", true)]
    public async Task Knowledge_editing_is_limited_to_company_managers(string role, bool allowed)
    {
        await using var factory = new IdentityTestFactory();
        var (owner, _) = await factory.RegisterAsync("roles-owner@example.test");
        var (member, memberId) = await factory.RegisterAsync("roles-member@example.test");
        var tenant = Guid.NewGuid();
        await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Role Company" });
        var invited = await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}/members/{memberId}/invitation", new { role });
        var invitation = (await invited.Content.ReadFromJsonAsync<MemberView>())!;
        await SendAsync(member, HttpMethod.Put, $"/api/invitations/{tenant}/accept", new { invitation.Version });
        var root = $"/api/tenants/{tenant}/company";
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync(root + "/profile")).StatusCode);
        var result = await SendAsync(member, HttpMethod.Put, root + "/profile", new SaveRecord<CompanyProfileData>(Guid.Empty, Guid.NewGuid(), Profile, "Owner-provided", null));
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, result.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RevolaAgent.Application.Audits;
using RevolaAgent.Application.Company;
using Xunit;
using static RevolaAgent.IntegrationTests.IdentityTestFactory;

namespace RevolaAgent.IntegrationTests;

public sealed class DemoAuditTests
{
    [Fact]
    public async Task Demo_results_are_reproducible_and_explicit_about_missing_data()
    {
        await using var factory = new IdentityTestFactory();
        var (owner, _) = await factory.RegisterAsync("audit-owner@example.test");
        var (other, _) = await factory.RegisterAsync("audit-other@example.test");
        var tenant = Guid.NewGuid(); var id = Guid.NewGuid();
        await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}", new { name = "Audit Example" });
        var root = $"/api/tenants/{tenant}/demo-audits";
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(owner, HttpMethod.Put, root + $"/{id}", new { scenario = "starter" })).StatusCode);
        await SendAsync(owner, HttpMethod.Put, $"/api/tenants/{tenant}/company/profile", new SaveRecord<CompanyProfileData>(Guid.Empty, Guid.NewGuid(), CompanyTests.Profile, "Owner", null));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(other, HttpMethod.Put, root + $"/{id}", new { scenario = "starter" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(root)).StatusCode);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await SendAsync(owner, HttpMethod.Put, root + $"/{id}", new { scenario = "starter" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<RevolaAgent.Application.Audits.AuditView>())!.Result;
            Assert.True(result.IsDemo); Assert.Equal(36, result.Score); Assert.Equal(5, result.AssessedCriteria);
            Assert.Equal(5, result.Criteria.Count(x => x.Score is null));
        }
        Assert.Single((await owner.GetFromJsonAsync<RevolaAgent.Application.Audits.AuditView[]>(root))!);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(owner, HttpMethod.Put, root + $"/{id}", new { scenario = "active" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(owner, HttpMethod.Put, root + $"/{Guid.NewGuid()}", new { scenario = "real-facebook" })).StatusCode);
    }

    [Theory]
    [InlineData(2, 1, false, false, 36)]
    [InlineData(8, 3, true, true, 100)]
    public void Rules_are_deterministic(int posts, int formats, bool image, bool contact, int expected)
    {
        var snapshot = new DemoAccount("test", posts, formats, image, contact);
        var first = DemoScoring.Evaluate(CompanyTests.Profile, snapshot);
        var second = DemoScoring.Evaluate(CompanyTests.Profile, snapshot);
        Assert.Equal(expected, first.Score);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal("demo-v1", first.RuleVersion);
    }
}

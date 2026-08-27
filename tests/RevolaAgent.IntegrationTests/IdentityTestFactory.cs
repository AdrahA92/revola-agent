using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RevolaAgent.Infrastructure.Persistence;
using Xunit;

namespace RevolaAgent.IntegrationTests;

// SQLite verifies relational behavior locally. PostgreSQL migrations are tested separately with Docker.
public sealed class IdentityTestFactory : WebApplicationFactory<Program>
{
    public const string Password = "Only-Test-Password-42!";
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string? postgresConnection;
    private readonly string environment;

    public IdentityTestFactory(string? postgresConnection = null, string environment = "Development")
    {
        this.postgresConnection = postgresConnection;
        this.environment = environment;
        if (postgresConnection is null) connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=unused");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RevolaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RevolaDbContext>>();
            services.AddDbContext<RevolaDbContext>(options =>
            {
                if (postgresConnection is null) options.UseSqlite(connection);
                else options.UseNpgsql(postgresConnection);
            });
        });
    }

    public HttpClient NewClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        { HandleCookies = true, AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RevolaDbContext>();
        if (postgresConnection is null) db.Database.EnsureCreated();
        else db.Database.Migrate();
        return client;
    }

    public async Task<(HttpClient Client, Guid Id)> RegisterAsync(string email)
    {
        var client = NewClient();
        var response = await SendAsync(client, HttpMethod.Post, "/api/identity/register", new { email, password = Password });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        response = await SendAsync(client, HttpMethod.Post, "/api/identity/login", new { email, password = Password });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/identity/me");
        return (client, me.GetProperty("id").GetGuid());
    }

    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/identity/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}

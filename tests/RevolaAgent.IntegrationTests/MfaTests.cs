using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace RevolaAgent.IntegrationTests;

public sealed class MfaTests
{
    [Fact]
    public async Task Mfa_requires_proof_revokes_sessions_and_rejects_code_replay()
    {
        await using var factory = new IdentityTestFactory();
        const string email = "mfa@example.test";
        var (client, _) = await factory.RegisterAsync(email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityTestFactory.SendAsync(client, HttpMethod.Post,
            "/api/identity/mfa/setup", new { password = "Wrong-test-password!" })).StatusCode);
        var setup = await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/mfa/setup", new { password = IdentityTestFactory.Password });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var key = (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sharedKey").GetString()!;
        var code = Totp(key);
        var enable = await IdentityTestFactory.SendAsync(client, HttpMethod.Post, "/api/identity/mfa/enable", new { password = IdentityTestFactory.Password, code });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        var codes = (await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recoveryCodes");
        Assert.Equal(10, codes.GetArrayLength());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/me")).StatusCode);
        using var login = factory.NewClient();
        var challenge = await IdentityTestFactory.SendAsync(login, HttpMethod.Post, "/api/identity/login", new { email, password = IdentityTestFactory.Password });
        Assert.Equal(HttpStatusCode.Accepted, challenge.StatusCode);
        Assert.True((await challenge.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requiresMfa").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await login.GetAsync("/api/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityTestFactory.SendAsync(login, HttpMethod.Post, "/api/identity/login", new { email, password = IdentityTestFactory.Password, code })).StatusCode);
        var recoveryCode = codes[0].GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(login, HttpMethod.Post, "/api/identity/login", new { email, password = IdentityTestFactory.Password, recoveryCode })).StatusCode);
        using var replay = factory.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await IdentityTestFactory.SendAsync(replay, HttpMethod.Post, "/api/identity/login", new { email, password = IdentityTestFactory.Password, recoveryCode })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(login, HttpMethod.Post, "/api/identity/mfa/disable", new { password = IdentityTestFactory.Password, recoveryCode = codes[1].GetString() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await login.GetAsync("/api/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IdentityTestFactory.SendAsync(replay, HttpMethod.Post, "/api/identity/login", new { email, password = IdentityTestFactory.Password })).StatusCode);
    }

    // RFC 6238 SHA-1, six digits, 30-second time step. Test-only independent client.
    private static string Totp(string key)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in key.TrimEnd('='))
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(buffer >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var offset = hash[^1] & 15;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7fffffff;
        return (value % 1000000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Api.Identity;

public static class IdentitySecurity
{
    public static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    public static string? Decode(string? token)
    {
        if (token is null || token.Length > 4096) return null;
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return null; }
    }

    public static async Task<bool> VerifySecondFactor(UserManager<ApplicationUser> users, ApplicationUser user, string? code, string? recoveryCode)
    {
        if (recoveryCode?.Length is > 0 and <= 100)
            return (await users.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode)).Succeeded;
        if (code?.Length != 6 || code.Any(c => c is < '0' or > '9')) return false;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.Id + ":" + code)));
        if (user.LastTwoFactorCodeHash == hash && user.LastTwoFactorCodeAt > DateTime.UtcNow.AddMinutes(-3)) return false;
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code)) return false;
        user.LastTwoFactorCodeHash = hash;
        user.LastTwoFactorCodeAt = DateTime.UtcNow;
        return (await users.UpdateAsync(user)).Succeeded;
    }

    public static Task<int> RevokeSessions(RevolaDbContext db, Guid userId, CancellationToken ct) =>
        db.LoginSessions.Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);
}

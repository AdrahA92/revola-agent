using Microsoft.AspNetCore.Identity;

namespace RevolaAgent.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? LastTwoFactorCodeHash { get; set; }
    public DateTime? LastTwoFactorCodeAt { get; set; }
}

using RevolaAgent.Domain.Tenancy;

namespace RevolaAgent.Application.Tenancy;

public sealed record TenantContext(Guid TenantId, Guid UserId, TenantRole Role);
public sealed record TenantView(Guid Id, string Name, string Role);
public sealed record MemberView(Guid UserId, string Role, bool Active, Guid Version);
public sealed record AuditView(Guid Id, Guid ActorId, Guid? SubjectId, string Action, DateTime OccurredAt);
public sealed record InvitationView(Guid TenantId, string Name, string Role, Guid Version);

public interface ITenancyService
{
    Task<IReadOnlyList<TenantView>> ListAsync(Guid userId, int page, CancellationToken ct);
    Task<TenantView> CreateAsync(Guid userId, Guid tenantId, string name, CancellationToken ct);
    Task<TenantContext> ResolveAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<TenantView> GetAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<MemberView>> MembersAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
    Task<MemberView> InviteAsync(Guid userId, Guid tenantId, Guid invitedUserId, TenantRole role, CancellationToken ct);
    Task<IReadOnlyList<InvitationView>> InvitationsAsync(Guid userId, int page, CancellationToken ct);
    Task AcceptAsync(Guid userId, Guid tenantId, Guid version, CancellationToken ct);
    Task<MemberView> ChangeRoleAsync(Guid userId, Guid tenantId, Guid memberId, TenantRole role, Guid version, CancellationToken ct);
    Task RemoveAsync(Guid userId, Guid tenantId, Guid memberId, Guid version, CancellationToken ct);
    Task<IReadOnlyList<AuditView>> AuditAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
}

public sealed class TenancyException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

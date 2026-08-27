namespace RevolaAgent.Domain.Tenancy;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum TenantRole { Owner, Admin, Manager, Editor, Approver, Viewer }

public sealed class Membership
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public TenantRole Role { get; set; }
    public bool Active { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

public static class MembershipPolicy
{
    public static bool CanManage(TenantRole actor, TenantRole target, TenantRole proposed) =>
        target != TenantRole.Owner && proposed != TenantRole.Owner &&
        (actor == TenantRole.Owner ||
         actor == TenantRole.Admin && target != TenantRole.Admin && proposed != TenantRole.Admin);
}

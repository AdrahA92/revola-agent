using RevolaAgent.Domain.Tenancy;
using Xunit;

namespace RevolaAgent.UnitTests;

public class MembershipPolicyTests
{
    [Theory]
    [InlineData(TenantRole.Owner, TenantRole.Viewer, TenantRole.Admin, true)]
    [InlineData(TenantRole.Admin, TenantRole.Editor, TenantRole.Manager, true)]
    [InlineData(TenantRole.Admin, TenantRole.Viewer, TenantRole.Admin, false)]
    [InlineData(TenantRole.Admin, TenantRole.Admin, TenantRole.Viewer, false)]
    [InlineData(TenantRole.Owner, TenantRole.Owner, TenantRole.Viewer, false)]
    [InlineData(TenantRole.Owner, TenantRole.Viewer, TenantRole.Owner, false)]
    [InlineData(TenantRole.Manager, TenantRole.Viewer, TenantRole.Editor, false)]
    [InlineData(TenantRole.Editor, TenantRole.Viewer, TenantRole.Editor, false)]
    [InlineData(TenantRole.Approver, TenantRole.Viewer, TenantRole.Editor, false)]
    [InlineData(TenantRole.Viewer, TenantRole.Viewer, TenantRole.Editor, false)]
    public void RoleManagementCannotEscalatePrivileges(TenantRole actor, TenantRole target, TenantRole proposed, bool expected)
        => Assert.Equal(expected, MembershipPolicy.CanManage(actor, target, proposed));
}

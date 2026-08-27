using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Infrastructure.Tenancy;

public sealed class TenancyService(RevolaDbContext db, UserManager<ApplicationUser> users) : ITenancyService
{
    private const int PageSize = 50;
    private static int Offset(int page) => page is >= 1 and <= 10000
        ? (page - 1) * PageSize : throw new TenancyException(400, "Ungültige Seite.");

    public async Task<IReadOnlyList<TenantView>> ListAsync(Guid userId, int page, CancellationToken ct) =>
        await (from member in db.Memberships.AsNoTracking()
               join tenant in db.Tenants on member.TenantId equals tenant.Id
               where member.UserId == userId && member.Active
               orderby tenant.Name, tenant.Id
               select new TenantView(tenant.Id, tenant.Name, member.Role.ToString()))
            .Skip(Offset(page)).Take(PageSize).ToListAsync(ct);

    public async Task<TenantContext> ResolveAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var member = await db.Memberships.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.UserId == userId && x.Active, ct);
        // Do not reveal whether a foreign tenant exists.
        return member is null ? throw new TenancyException(404, "Organisation nicht gefunden.")
            : new TenantContext(tenantId, userId, member.Role);
    }

    public async Task<TenantView> CreateAsync(Guid userId, Guid tenantId, string name, CancellationToken ct)
    {
        name = name.Trim();
        if (tenantId == Guid.Empty || name.Length is < 2 or > 160)
            throw new TenancyException(400, "Organisations-ID und Name sind erforderlich (2–160 Zeichen).");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, ct);
        if (existing is not null)
        {
            if (existing.CreatedBy != userId || existing.Name != name)
                throw new TenancyException(409, "Organisations-ID bereits verwendet.");
            var context = await ResolveAsync(userId, tenantId, ct);
            return new TenantView(tenantId, existing.Name, context.Role.ToString());
        }
        db.Tenants.Add(new Tenant { Id = tenantId, Name = name, CreatedBy = userId, CreatedAt = DateTimeOffset.UtcNow });
        db.Memberships.Add(new Membership { TenantId = tenantId, UserId = userId, Role = TenantRole.Owner, Active = true });
        AddAudit(tenantId, userId, userId, "tenant.created");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new TenantView(tenantId, name, TenantRole.Owner.ToString());
    }

    public async Task<IReadOnlyList<MemberView>> MembersAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        var context = await ResolveAsync(userId, tenantId, ct);
        RequireManager(context);
        return await db.Memberships.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.UserId)
            .Skip(Offset(page)).Take(PageSize)
            .Select(x => new MemberView(x.UserId, x.Role.ToString(), x.Active, x.Version)).ToListAsync(ct);
    }

    public async Task<MemberView> InviteAsync(Guid userId, Guid tenantId, Guid invitedUserId, TenantRole role, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await ResolveAsync(userId, tenantId, ct);
        if (!Enum.IsDefined(role) || !MembershipPolicy.CanManage(context.Role, role, role))
            throw new TenancyException(403, "Rolle darf nicht zugewiesen werden.");
        if (await users.FindByIdAsync(invitedUserId.ToString()) is null)
            throw new TenancyException(400, "Benutzer-ID ist nicht verfügbar.");
        var member = await db.Memberships.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == invitedUserId, ct);
        if (member is not null)
        {
            if (member.Role != role) throw new TenancyException(409, "Mitgliedschaft existiert bereits mit einer anderen Rolle.");
            return View(member);
        }
        member = new Membership { TenantId = tenantId, UserId = invitedUserId, Role = role, Active = false };
        db.Memberships.Add(member);
        AddAudit(tenantId, userId, invitedUserId, "membership.invited");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return View(member);
    }

    public async Task<IReadOnlyList<InvitationView>> InvitationsAsync(Guid userId, int page, CancellationToken ct) =>
        await (from member in db.Memberships.AsNoTracking()
               join tenant in db.Tenants on member.TenantId equals tenant.Id
               where member.UserId == userId && !member.Active
               orderby tenant.Name, tenant.Id
               select new InvitationView(tenant.Id, tenant.Name, member.Role.ToString(), member.Version))
            .Skip(Offset(page)).Take(PageSize).ToListAsync(ct);

    public async Task AcceptAsync(Guid userId, Guid tenantId, Guid version, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var member = await FindMember(tenantId, userId, ct);
        if (member.Active) return; // An already accepted invitation is idempotent.
        CheckVersion(member, version);
        member.Active = true;
        member.Version = Guid.NewGuid();
        AddAudit(tenantId, userId, userId, "membership.accepted");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<MemberView> ChangeRoleAsync(Guid userId, Guid tenantId, Guid memberId, TenantRole role, Guid version, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await ResolveAsync(userId, tenantId, ct);
        var member = await FindMember(tenantId, memberId, ct);
        if (!Enum.IsDefined(role) || !MembershipPolicy.CanManage(context.Role, member.Role, role))
            throw new TenancyException(403, "Rolle darf nicht geändert werden.");
        CheckVersion(member, version);
        member.Role = role;
        member.Version = Guid.NewGuid();
        AddAudit(tenantId, userId, memberId, "membership.role_changed");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return View(member);
    }

    public async Task RemoveAsync(Guid userId, Guid tenantId, Guid memberId, Guid version, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await ResolveAsync(userId, tenantId, ct);
        RequireManager(context);
        var member = await db.Memberships.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == memberId, ct);
        if (member is null) return;
        if (!MembershipPolicy.CanManage(context.Role, member.Role, member.Role))
            throw new TenancyException(403, "Mitglied darf nicht entfernt werden.");
        CheckVersion(member, version);
        db.Memberships.Remove(member);
        AddAudit(tenantId, userId, memberId, "membership.removed");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<AuditView>> AuditAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        RequireManager(await ResolveAsync(userId, tenantId, ct));
        return await db.AuditEvents.AsNoTracking().Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Id).Skip(Offset(page)).Take(PageSize)
            .Select(x => new AuditView(x.Id, x.ActorId, x.SubjectId, x.Action, x.OccurredAt)).ToListAsync(ct);
    }

    private async Task<Membership> FindMember(Guid tenantId, Guid memberId, CancellationToken ct) =>
        await db.Memberships.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == memberId, ct)
            ?? throw new TenancyException(404, "Mitgliedschaft nicht gefunden.");

    private static void RequireManager(TenantContext context)
    {
        if (context.Role is not (TenantRole.Owner or TenantRole.Admin))
            throw new TenancyException(403, "Berechtigung fehlt.");
    }

    private static void CheckVersion(Membership member, Guid version)
    {
        if (member.Version != version) throw new TenancyException(409, "Mitgliedschaft wurde zwischenzeitlich geändert.");
    }

    private void AddAudit(Guid tenantId, Guid actorId, Guid subjectId, string action) =>
        db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = actorId, SubjectId = subjectId, Action = action });

    private static MemberView View(Membership member) => new(member.UserId, member.Role.ToString(), member.Active, member.Version);
}

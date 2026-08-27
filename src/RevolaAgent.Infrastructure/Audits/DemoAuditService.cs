using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Application.Audits;
using RevolaAgent.Application.Company;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Audits;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Persistence;
using AuditView = RevolaAgent.Application.Audits.AuditView;

namespace RevolaAgent.Infrastructure.Audits;

public sealed class DemoPlatform : IDemoPlatform
{
    public DemoAccount Read(string scenario) => scenario switch
    {
        "starter" => new(scenario, 2, 1, false, false),
        "active" => new(scenario, 8, 3, true, true),
        _ => throw new TenancyException(400, "Unbekanntes Demo-Szenario.")
    };
}

public sealed class DemoAuditService(RevolaDbContext db, ITenancyService tenancy, ICompanyService company, IDemoPlatform platform) : IAuditService
{
    public async Task<AuditView> RunAsync(Guid userId, Guid tenantId, Guid id, string scenario, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await tenancy.ResolveAsync(userId, tenantId, ct);
        if (context.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Manager)) throw new TenancyException(403, "Keine Berechtigung für Auditläufe.");
        if (id == Guid.Empty) throw new TenancyException(400, "Audit-ID fehlt.");
        var snapshot = platform.Read(scenario);
        var existing = await db.AuditRuns.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (existing is not null)
        {
            if (existing.Scenario != scenario) throw new TenancyException(409, "Audit-ID bereits verwendet.");
            return View(existing);
        }
        var profile = await company.ProfileAsync(userId, tenantId, ct) ?? throw new TenancyException(409, "Zuerst Unternehmensprofil vervollständigen.");
        if (profile.ExpiresAt <= DateTime.UtcNow) throw new TenancyException(409, "Unternehmensprofil ist abgelaufen.");
        var result = DemoScoring.Evaluate(profile.Data, snapshot);
        var run = new AuditRun { TenantId = tenantId, Id = id, ActorId = userId, ProfileVersion = profile.Version,
            Scenario = scenario, RuleVersion = DemoScoring.Version, SnapshotJson = JsonSerializer.Serialize(new { Profile = profile, Account = snapshot }),
            ResultJson = JsonSerializer.Serialize(result), CreatedAt = DateTime.UtcNow };
        db.AuditRuns.Add(run);
        db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = userId, SubjectId = id, Action = "audit.demo_completed" });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return View(run);
    }

    public async Task<IReadOnlyList<AuditView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct);
        if (page is < 1 or > 10000) throw new TenancyException(400, "Ungültige Seite.");
        var runs = await db.AuditRuns.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return runs.Select(View).ToArray();
    }
    private static AuditView View(AuditRun run) => new(run.Id, run.ProfileVersion, run.Scenario, run.CreatedAt, JsonSerializer.Deserialize<AuditResult>(run.ResultJson)!);
}

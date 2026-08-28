using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Application.Agents;
using RevolaAgent.Application.Company;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Agents;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Infrastructure.Agents;

public sealed class DemoDraftGenerator : IDraftGenerator
{
    public string Model => "demo-template-v1";
    public Task<DraftResult> GenerateAsync(DraftInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Deterministic test double, not an AI response. User content never selects executable tools.
        var name = input.Profile.Name;
        var introduction = input.Platform == "demo-linkedin" ? "Ein Einblick in unser Unternehmen" : "Lernen Sie uns kennen";
        return Task.FromResult(new DraftResult(name,
            $"[DEMO-ENTWURF – bitte prüfen]\n{introduction}: {name}.\n{input.Profile.Description[..Math.Min(input.Profile.Description.Length, 2000)]}\nKontakt: {input.Profile.Email}\n{input.Profile.Website}",
            "Sachliche Unternehmensillustration in der hinterlegten Markenfarbe. Keine erfundenen Kundenlogos, Referenzen oder Leistungsversprechen.",
            "Geplante Unternehmensillustration; tatsächliches Bild und Alternativtext vor Veröffentlichung prüfen."));
    }
}

public sealed class AgentService(RevolaDbContext db, ITenancyService tenancy, ICompanyService company, IDraftGenerator generator) : IAgentService
{
    public async Task<AgentRunView> RunAsync(Guid userId, Guid tenantId, Guid id, string goal, string platform, CancellationToken ct)
    {
        AgentRun run;
        RecordView<CompanyProfileData> profile;
        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            await Authorize(userId, tenantId, ct);
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(goal) || goal.Length > 2000 || !AgentPolicy.IsDemoPlatform(platform))
                throw new TenancyException(400, "Ungültiges Briefing oder kein Demo-Ziel.");
            var existing = await db.AgentRuns.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
            if (existing is not null)
            {
                if (existing.Goal != goal || existing.Platform != platform) throw new TenancyException(409, "Lauf-ID bereits verwendet.");
                return View(existing);
            }
            var now = DateTime.UtcNow; var day = now.Date;
            if (await db.AgentRuns.CountAsync(x => x.TenantId == tenantId && x.CreatedAt >= day, ct) >= AgentPolicy.DailyRuns ||
                await db.AgentRuns.CountAsync(x => x.TenantId == tenantId && x.Status == "Running" && x.Deadline > now, ct) >= AgentPolicy.ConcurrentRuns)
                throw new TenancyException(429, "Testkontingent ausgeschöpft.");
            profile = await company.ProfileAsync(userId, tenantId, ct) ?? throw new TenancyException(409, "Unternehmensprofil fehlt.");
            if (profile.ExpiresAt <= now) throw new TenancyException(409, "Unternehmensprofil ist abgelaufen.");
            run = new AgentRun { TenantId = tenantId, Id = id, ActorId = userId, ProfileVersion = profile.Version,
                Goal = goal, Platform = platform, Model = generator.Model, CreatedAt = now, Deadline = now.AddSeconds(AgentPolicy.TimeoutSeconds),
                StepsJson = JsonSerializer.Serialize(new[] { new AgentStepView("get_company_profile", "ReadOnly", "Completed") }) };
            db.AgentRuns.Add(run);
            db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = userId, SubjectId = id, Action = "agent.started" });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(AgentPolicy.TimeoutSeconds));
        DraftResult? draft = null;
        try
        {
            var result = await generator.GenerateAsync(new(goal, platform, profile.Data), timeout.Token).WaitAsync(timeout.Token);
            if (!AgentPolicy.IsValid(result)) throw new InvalidOperationException("Invalid draft output.");
            draft = result;
        }
        catch (OperationCanceledException) { run.Status = ct.IsCancellationRequested ? "Cancelled" : "TimedOut"; run.ErrorCode = "execution_cancelled"; }
        catch (Exception) { run.Status = "Failed"; run.ErrorCode = "draft_failed"; /* No provider response or input in logs. */ }
        using var persistTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var completion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, persistTimeout.Token);
        if (draft is not null)
        {
            try
            {
                await Authorize(userId, tenantId, persistTimeout.Token);
                run.ResultJson = JsonSerializer.Serialize(draft); run.Status = "Completed";
            }
            catch (TenancyException) { run.ResultJson = null; run.Status = "Failed"; run.ErrorCode = "permission_revoked"; }
        }
        run.StepsJson = JsonSerializer.Serialize(new[] { new AgentStepView("get_company_profile", "ReadOnly", "Completed"), new AgentStepView("draft_content", "Draft", run.Status) });
        run.CompletedAt = DateTime.UtcNow; run.Version = Guid.NewGuid();
        db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = userId, SubjectId = id, Action = "agent." + run.Status.ToLowerInvariant() });
        await db.SaveChangesAsync(persistTimeout.Token);
        await completion.CommitAsync(persistTimeout.Token);
        await tenancy.ResolveAsync(userId, tenantId, ct);
        return View(run);
    }

    public async Task<IReadOnlyList<AgentRunView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct);
        if (page is < 1 or > 10000) throw new TenancyException(400, "Ungültige Seite.");
        var rows = await db.AgentRuns.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return rows.Select(View).ToArray();
    }
    private async Task Authorize(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var context = await tenancy.ResolveAsync(userId, tenantId, ct);
        if (context.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Manager or TenantRole.Editor))
            throw new TenancyException(403, "Keine Berechtigung für Entwürfe.");
    }
    private static AgentRunView View(AgentRun run) => new(run.Id, run.ProfileVersion, run.Goal, run.Platform,
        run.Status == "Running" && run.Deadline <= DateTime.UtcNow ? "TimedOut" : run.Status, run.Model,
        run.ResultJson is null ? null : JsonSerializer.Deserialize<DraftResult>(run.ResultJson), run.ErrorCode,
        run.InputTokens, run.OutputTokens, run.Cost, run.CreatedAt, run.CompletedAt,
        JsonSerializer.Deserialize<AgentStepView[]>(run.StepsJson)!);
}

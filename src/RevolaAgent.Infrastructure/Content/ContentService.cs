using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Application.Content;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Content;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Infrastructure.Content;

public sealed class ContentService(RevolaDbContext db, ITenancyService tenancy) : IContentService
{
    public async Task<ContentView> SaveAsync(Guid userId, Guid tenantId, Guid id, SaveContent request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var actor = await tenancy.ResolveAsync(userId, tenantId, ct); RequireEditor(actor);
        if (id == Guid.Empty || request.NewVersion == Guid.Empty || request.Data is null || !ContentValidation.Valid(request.Data)) Invalid();
        var data = request.Data!;
        try { if (data.TimeZone != "UTC" && !data.TimeZone.Contains('/')) Invalid(); TimeZoneInfo.FindSystemTimeZoneById(data.TimeZone); }
        catch (TimeZoneNotFoundException) { Invalid(); }
        catch (InvalidTimeZoneException) { Invalid(); }
        var hash = Hash(data);
        var item = await db.ContentItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (item is not null && item.Version == request.NewVersion)
        {
            var previous = await Version(tenantId, id, item.Version, ct);
            if (previous.Hash != hash) Conflict();
            return View(item, previous);
        }
        if (data.ScheduledAt <= DateTime.UtcNow || data.ScheduledAt > DateTime.UtcNow.AddYears(1)) Invalid();
        if (request.NewVersion == request.Version || await db.ContentVersions.AnyAsync(x => x.TenantId == tenantId && x.ContentId == id && x.Version == request.NewVersion, ct)) Conflict();
        if (item is null)
        {
            if (request.Version != Guid.Empty) Conflict();
            item = new ContentItem { TenantId = tenantId, Id = id }; db.ContentItems.Add(item);
        }
        else if (item.Version != request.Version) Conflict();
        item.Version = request.NewVersion; item.Status = "Draft"; item.StateVersion = Guid.NewGuid();
        item.ApprovedBy = null; item.ApprovalExpiresAt = null; item.ApprovedHash = null; item.UpdatedAt = DateTime.UtcNow;
        var version = new ContentVersion { TenantId = tenantId, ContentId = id, Version = item.Version, AuthorId = userId,
            Title = data.Title, Text = data.Text, ImageBrief = data.ImageBrief, AltText = data.AltText, Target = data.Target,
            ScheduledAt = data.ScheduledAt, TimeZone = data.TimeZone, Hash = hash, CreatedAt = item.UpdatedAt };
        db.ContentVersions.Add(version); Audit(tenantId, userId, id, "content.version_saved");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return View(item, version);
    }

    public async Task<ContentView> TransitionAsync(Guid userId, Guid tenantId, Guid id, DecisionRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var actor = await tenancy.ResolveAsync(userId, tenantId, ct);
        var item = await db.ContentItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct) ?? throw new TenancyException(404, "Entwurf nicht verfügbar.");
        if (item.Version != request.Version) Conflict();
        var version = await Version(tenantId, id, item.Version, ct);
        var now = DateTime.UtcNow;
        switch (request.Decision)
        {
            case "submit":
                RequireEditor(actor);
                if (item.Status == "InReview") return View(item, version);
                if (item.Status is not ("Draft" or "Rejected")) Conflict();
                item.Status = "InReview"; break;
            case "approve":
            case "reject":
                RequireApprover(actor);
                if (version.AuthorId == userId) throw new TenancyException(403, "Vier-Augen-Prinzip: Eine andere Person muss prüfen.");
                if (request.Decision == "approve" && item.Status == "Approved" && item.ApprovedBy == userId && item.ApprovalExpiresAt == request.ExpiresAt) return View(item, version);
                if (item.Status != "InReview") Conflict();
                if (request.Decision == "approve")
                {
                    if (request.ExpiresAt is null || request.ExpiresAt.Value.Kind != DateTimeKind.Utc || request.ExpiresAt <= now || request.ExpiresAt > now.AddDays(7) || request.ExpiresAt <= version.ScheduledAt) Invalid();
                    item.Status = "Approved"; item.ApprovedBy = userId; item.ApprovalExpiresAt = request.ExpiresAt; item.ApprovedHash = version.Hash;
                }
                else { item.Status = "Rejected"; item.ApprovedBy = null; item.ApprovalExpiresAt = null; item.ApprovedHash = null; }
                break;
            case "schedule":
                if (actor.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Manager)) throw new TenancyException(403, "Keine Planungsberechtigung.");
                if (item.Status is not ("Approved" or "Scheduled") || item.ApprovedBy is null || item.ApprovalExpiresAt is null || item.ApprovalExpiresAt <= now ||
                    item.ApprovalExpiresAt <= version.ScheduledAt || version.ScheduledAt <= now || item.ApprovedHash != version.Hash) Conflict();
                RequireApprover(await tenancy.ResolveAsync(item.ApprovedBy!.Value, tenantId, ct));
                if (item.Status == "Scheduled") return View(item, version);
                item.Status = "Scheduled"; break;
            case "cancel":
                RequireEditor(actor);
                if (item.Status == "Cancelled") return View(item, version);
                item.Status = "Cancelled"; item.ApprovedBy = null; item.ApprovalExpiresAt = null; item.ApprovedHash = null; break;
            default: Invalid(); break;
        }
        item.StateVersion = Guid.NewGuid(); item.UpdatedAt = now;
        db.ContentDecisions.Add(new ContentDecision { TenantId = tenantId, ContentId = id, Version = item.Version,
            ActorId = userId, Decision = request.Decision, Hash = version.Hash, ExpiresAt = item.ApprovalExpiresAt, CreatedAt = now });
        Audit(tenantId, userId, id, "content." + request.Decision);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return View(item, version);
    }

    public async Task<IReadOnlyList<ContentView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct); Page(page);
        var rows = await (from item in db.ContentItems.AsNoTracking()
            join version in db.ContentVersions.AsNoTracking() on new { item.TenantId, ContentId = item.Id, item.Version }
                equals new { version.TenantId, version.ContentId, version.Version }
            where item.TenantId == tenantId orderby version.ScheduledAt, item.Id
            select new { Item = item, Version = version }).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return rows.Select(x => View(x.Item, x.Version)).ToArray();
    }
    public async Task<IReadOnlyList<ContentHistoryView>> HistoryAsync(Guid userId, Guid tenantId, Guid id, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct); Page(page);
        if (!await db.ContentItems.AnyAsync(x => x.TenantId == tenantId && x.Id == id, ct)) throw new TenancyException(404, "Entwurf nicht verfügbar.");
        var rows = await db.ContentVersions.AsNoTracking().Where(x => x.TenantId == tenantId && x.ContentId == id)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Version).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return rows.Select(x => new ContentHistoryView(x.Version, x.AuthorId, Data(x), x.Hash, x.CreatedAt)).ToArray();
    }
    private Task<ContentVersion> Version(Guid tenantId, Guid id, Guid version, CancellationToken ct) =>
        db.ContentVersions.AsNoTracking().SingleAsync(x => x.TenantId == tenantId && x.ContentId == id && x.Version == version, ct);
    private static ContentData Data(ContentVersion v) => new(v.Title, v.Text, v.ImageBrief, v.AltText, v.Target, v.ScheduledAt, v.TimeZone);
    private static ContentView View(ContentItem item, ContentVersion version) => new(item.Id, item.Version, version.AuthorId, item.Status, Data(version), version.Hash, item.ApprovedBy, item.ApprovalExpiresAt, true);
    private static string Hash(ContentData data) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
    private void Audit(Guid tenantId, Guid userId, Guid id, string action) => db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = userId, SubjectId = id, Action = action });
    private static void RequireEditor(TenantContext actor) { if (actor.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Manager or TenantRole.Editor)) throw new TenancyException(403, "Keine Bearbeitungsberechtigung."); }
    private static void RequireApprover(TenantContext actor) { if (actor.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Approver)) throw new TenancyException(403, "Keine Freigabeberechtigung."); }
    private static void Page(int page) { if (page is < 1 or > 10000) Invalid(); }
    private static void Invalid() => throw new TenancyException(400, "Bitte prüfen Sie Inhalt, Ziel und UTC-Zeitpunkte.");
    private static void Conflict() => throw new TenancyException(409, "Status, Version oder Freigabe ist nicht mehr gültig.");
}

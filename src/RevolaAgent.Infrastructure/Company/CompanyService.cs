using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RevolaAgent.Application.Company;
using RevolaAgent.Application.Tenancy;
using RevolaAgent.Domain.Company;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Persistence;

namespace RevolaAgent.Infrastructure.Company;

public sealed class CompanyService(RevolaDbContext db, ITenancyService tenancy) : ICompanyService
{
    public async Task<RecordView<CompanyProfileData>?> ProfileAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct);
        var record = await db.CompanyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == tenantId && x.Kind == "profile", ct);
        return record is null ? null : View<CompanyProfileData>(record);
    }

    public Task<RecordView<CompanyProfileData>> SaveProfileAsync(Guid userId, Guid tenantId, SaveRecord<CompanyProfileData> request, CancellationToken ct) =>
        SaveAsync(userId, tenantId, tenantId, "profile", request, data =>
        {
            Required(data.Name, 160); Required(data.Industry, 160); Required(data.Description, 4000);
            Required(data.Services, 4000); Required(data.Audience, 2000); Required(data.Regions, 1000);
            Optional(data.Tone, 1000); Optional(data.AllowedClaims, 4000); Optional(data.ForbiddenClaims, 4000); Optional(data.Goals, 2000);
            if (data.Email is null || data.Email.Length > 254 || !new EmailAddressAttribute().IsValid(data.Email)) Invalid();
            if (data.Website is null || data.Website.Length > 2048 || !Uri.TryCreate(data.Website, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)) Invalid();
            if (data.BrandColor is null || !Regex.IsMatch(data.BrandColor, "^#[a-fA-F0-9]{6}$")) Invalid();
        }, ct);

    public async Task<IReadOnlyList<RecordView<KnowledgeData>>> KnowledgeAsync(Guid userId, Guid tenantId, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct); Page(page);
        var records = await db.CompanyRecords.AsNoTracking().Where(x => x.TenantId == tenantId && x.Kind == "knowledge")
            .OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(ct);
        return records.Select(View<KnowledgeData>).ToArray();
    }

    public Task<RecordView<KnowledgeData>> SaveKnowledgeAsync(Guid userId, Guid tenantId, Guid id, SaveRecord<KnowledgeData> request, CancellationToken ct) =>
        SaveAsync(userId, tenantId, id, "knowledge", request, data => { Required(data.Title, 160); Required(data.Content, 8000); }, ct);

    public async Task<IReadOnlyList<RevisionView>> HistoryAsync(Guid userId, Guid tenantId, Guid id, int page, CancellationToken ct)
    {
        await tenancy.ResolveAsync(userId, tenantId, ct); Page(page);
        if (!await db.CompanyRecords.AnyAsync(x => x.TenantId == tenantId && x.Id == id, ct)) throw new TenancyException(404, "Eintrag nicht verfügbar.");
        return await db.CompanyRevisions.AsNoTracking().Where(x => x.TenantId == tenantId && x.RecordId == id)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Version).Skip((page - 1) * 50).Take(50)
            .Select(x => new RevisionView(x.Version, x.ActorId, x.DataJson, x.Source, x.CreatedAt, x.ExpiresAt)).ToListAsync(ct);
    }

    private async Task<RecordView<T>> SaveAsync<T>(Guid userId, Guid tenantId, Guid id, string kind, SaveRecord<T> request, Action<T> validate, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await tenancy.ResolveAsync(userId, tenantId, ct);
        if (context.Role is not (TenantRole.Owner or TenantRole.Admin or TenantRole.Manager)) throw new TenancyException(403, "Keine Schreibberechtigung.");
        if (id == Guid.Empty || request.NewVersion == Guid.Empty || request.Data is null || (kind == "knowledge" && id == tenantId)) Invalid();
        Required(request.Source, 2000); validate(request.Data!);
        if (request.ExpiresAt is { Kind: not DateTimeKind.Utc }) Invalid();
        var data = JsonSerializer.Serialize(request.Data);
        if (data.Length > 32000) Invalid();
        var record = await db.CompanyRecords.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (record is not null && record.Version == request.NewVersion && record.Kind == kind && record.DataJson == data && record.Source == request.Source && record.ExpiresAt == request.ExpiresAt)
            return View<T>(record); // Identical retry, no duplicate revision or audit event.
        if (request.NewVersion == request.Version || await db.CompanyRevisions.AnyAsync(x => x.TenantId == tenantId && x.RecordId == id && x.Version == request.NewVersion, ct))
            throw new TenancyException(409, "Version bereits verwendet.");
        if (record is null)
        {
            if (request.Version != Guid.Empty) throw new TenancyException(409, "Eintrag wurde geändert.");
            record = new CompanyRecord { TenantId = tenantId, Id = id, Kind = kind };
            db.CompanyRecords.Add(record);
        }
        else if (record.Version != request.Version || record.Kind != kind) throw new TenancyException(409, "Eintrag wurde geändert.");
        record.Version = request.NewVersion; record.DataJson = data; record.Source = request.Source;
        record.UpdatedAt = DateTime.UtcNow; record.ExpiresAt = request.ExpiresAt;
        db.CompanyRevisions.Add(new CompanyRevision { TenantId = tenantId, RecordId = id, Version = record.Version,
            ActorId = userId, DataJson = data, Source = record.Source, CreatedAt = record.UpdatedAt, ExpiresAt = record.ExpiresAt });
        db.AuditEvents.Add(new AuditEvent { TenantId = tenantId, ActorId = userId, SubjectId = id, Action = "company." + kind + "_saved" });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return View<T>(record);
    }

    private static RecordView<T> View<T>(CompanyRecord record) => new(record.Id, record.Version,
        JsonSerializer.Deserialize<T>(record.DataJson)!, record.Source, record.UpdatedAt, record.ExpiresAt);
    private static void Required(string? value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Length > max) Invalid(); }
    private static void Optional(string? value, int max) { if (value is null || value.Length > max) Invalid(); }
    private static void Page(int page) { if (page is < 1 or > 10000) Invalid(); }
    private static void Invalid() => throw new TenancyException(400, "Bitte prüfen Sie die Unternehmensdaten.");
}

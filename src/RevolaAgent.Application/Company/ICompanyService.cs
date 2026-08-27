namespace RevolaAgent.Application.Company;

public sealed record CompanyProfileData(string Name, string Industry, string Description, string Email,
    string Website, string Services, string Audience, string Regions, string BrandColor, string Tone,
    string AllowedClaims, string ForbiddenClaims, string Goals);
public sealed record KnowledgeData(string Title, string Content);
public sealed record RecordView<T>(Guid Id, Guid Version, T Data, string Source, DateTime UpdatedAt, DateTime? ExpiresAt);
public sealed record SaveRecord<T>(Guid Version, Guid NewVersion, T Data, string Source, DateTime? ExpiresAt);
public sealed record RevisionView(Guid Version, Guid ActorId, string DataJson, string Source, DateTime CreatedAt, DateTime? ExpiresAt);

public interface ICompanyService
{
    Task<RecordView<CompanyProfileData>?> ProfileAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<RecordView<CompanyProfileData>> SaveProfileAsync(Guid userId, Guid tenantId, SaveRecord<CompanyProfileData> request, CancellationToken ct);
    Task<IReadOnlyList<RecordView<KnowledgeData>>> KnowledgeAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
    Task<RecordView<KnowledgeData>> SaveKnowledgeAsync(Guid userId, Guid tenantId, Guid id, SaveRecord<KnowledgeData> request, CancellationToken ct);
    Task<IReadOnlyList<RevisionView>> HistoryAsync(Guid userId, Guid tenantId, Guid id, int page, CancellationToken ct);
}

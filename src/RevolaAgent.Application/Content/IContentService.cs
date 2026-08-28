using RevolaAgent.Application.Agents;

namespace RevolaAgent.Application.Content;

public sealed record ContentData(string Title, string Text, string ImageBrief, string AltText, string Target, DateTime ScheduledAt, string TimeZone);
public sealed record SaveContent(Guid Version, Guid NewVersion, ContentData Data);
public sealed record ContentView(Guid Id, Guid Version, Guid AuthorId, string Status, ContentData Data, string Hash,
    Guid? ApprovedBy, DateTime? ApprovalExpiresAt, bool IsDemo);
public sealed record DecisionRequest(Guid Version, string Decision, DateTime? ExpiresAt);
public sealed record ContentHistoryView(Guid Version, Guid AuthorId, ContentData Data, string Hash, DateTime CreatedAt);
public interface IContentService
{
    Task<IReadOnlyList<ContentView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
    Task<ContentView> SaveAsync(Guid userId, Guid tenantId, Guid id, SaveContent request, CancellationToken ct);
    Task<ContentView> TransitionAsync(Guid userId, Guid tenantId, Guid id, DecisionRequest request, CancellationToken ct);
    Task<IReadOnlyList<ContentHistoryView>> HistoryAsync(Guid userId, Guid tenantId, Guid id, int page, CancellationToken ct);
}

public static class ContentValidation
{
    public static bool Valid(ContentData data) => AgentPolicy.IsValid(new DraftResult(data.Title, data.Text, data.ImageBrief, data.AltText)) &&
        AgentPolicy.IsDemoPlatform(data.Target) && data.ScheduledAt.Kind == DateTimeKind.Utc &&
        data.TimeZone is { Length: > 0 and <= 100 };
}

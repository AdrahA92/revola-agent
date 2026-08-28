using RevolaAgent.Application.Company;

namespace RevolaAgent.Application.Agents;

public sealed record DraftResult(string Title, string Text, string ImageBrief, string AltText);
public sealed record DraftInput(string Goal, string Platform, CompanyProfileData Profile);
public interface IDraftGenerator
{
    string Model { get; }
    Task<DraftResult> GenerateAsync(DraftInput input, CancellationToken ct);
}
public sealed record AgentRunView(Guid Id, Guid ProfileVersion, string Goal, string Platform, string Status,
    string Model, DraftResult? Result, string? ErrorCode, int InputTokens, int OutputTokens, decimal Cost,
    DateTime CreatedAt, DateTime? CompletedAt, IReadOnlyList<AgentStepView> Steps);
public sealed record AgentStepView(string Tool, string Risk, string Status);
public interface IAgentService
{
    Task<AgentRunView> RunAsync(Guid userId, Guid tenantId, Guid id, string goal, string platform, CancellationToken ct);
    Task<IReadOnlyList<AgentRunView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
}

public static class AgentPolicy
{
    public const int DailyRuns = 20;
    public const int ConcurrentRuns = 2;
    public const int TimeoutSeconds = 15;
    public static bool IsAllowedTool(string name) => name is "get_company_profile" or "draft_content";
    public static bool IsDemoPlatform(string platform) => platform is "demo-facebook" or "demo-linkedin";
    public static bool IsValid(DraftResult result) => !string.IsNullOrWhiteSpace(result.Title) && result.Title.Length <= 160 &&
        !string.IsNullOrWhiteSpace(result.Text) && result.Text.Length <= 5000 &&
        !string.IsNullOrWhiteSpace(result.ImageBrief) && result.ImageBrief.Length <= 2000 &&
        !string.IsNullOrWhiteSpace(result.AltText) && result.AltText.Length <= 500;
}

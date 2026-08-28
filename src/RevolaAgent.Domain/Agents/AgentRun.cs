namespace RevolaAgent.Domain.Agents;

public sealed class AgentRun
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid ProfileVersion { get; set; }
    public string Goal { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Status { get; set; } = "Running";
    public string Model { get; set; } = "demo-template-v1";
    public string? ResultJson { get; set; }
    public string StepsJson { get; set; } = "[]";
    public string? ErrorCode { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

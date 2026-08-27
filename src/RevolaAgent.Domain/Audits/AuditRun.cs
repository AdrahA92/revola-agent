namespace RevolaAgent.Domain.Audits;

public sealed class AuditRun
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid ProfileVersion { get; set; }
    public string Scenario { get; set; } = "starter";
    public string RuleVersion { get; set; } = "demo-v1";
    public string SnapshotJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}

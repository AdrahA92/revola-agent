namespace RevolaAgent.Domain.Company;

public sealed class CompanyRecord
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid Version { get; set; }
    public string Kind { get; set; } = "profile";
    public string DataJson { get; set; } = "{}";
    public string Source { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class CompanyRevision
{
    public Guid TenantId { get; set; }
    public Guid RecordId { get; set; }
    public Guid Version { get; set; }
    public Guid ActorId { get; set; }
    public string DataJson { get; set; } = "{}";
    public string Source { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

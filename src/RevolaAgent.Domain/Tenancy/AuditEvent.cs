namespace RevolaAgent.Domain.Tenancy;

// Contains identifiers and allowlisted actions only, never passwords, email or request bodies.
public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid ActorId { get; set; }
    public Guid? SubjectId { get; set; }
    public string Action { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

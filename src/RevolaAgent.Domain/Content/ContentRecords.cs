namespace RevolaAgent.Domain.Content;

public sealed class ContentItem
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid Version { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovalExpiresAt { get; set; }
    public string? ApprovedHash { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid StateVersion { get; set; } = Guid.NewGuid();
}

public sealed class ContentVersion
{
    public Guid TenantId { get; set; }
    public Guid ContentId { get; set; }
    public Guid Version { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string ImageBrief { get; set; } = "";
    public string AltText { get; set; } = "";
    public string Target { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public string TimeZone { get; set; } = "Europe/Berlin";
    public string Hash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class ContentDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ContentId { get; set; }
    public Guid Version { get; set; }
    public Guid ActorId { get; set; }
    public string Decision { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

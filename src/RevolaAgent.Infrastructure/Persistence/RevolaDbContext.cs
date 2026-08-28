using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RevolaAgent.Domain.Tenancy;
using RevolaAgent.Infrastructure.Identity;
using RevolaAgent.Domain.Company;
using RevolaAgent.Domain.Audits;
using RevolaAgent.Domain.Agents;
using RevolaAgent.Domain.Content;

namespace RevolaAgent.Infrastructure.Persistence;

// Migrations are applied explicitly, never on API startup.
public sealed class RevolaDbContext(DbContextOptions<RevolaDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
    public DbSet<CompanyRecord> CompanyRecords => Set<CompanyRecord>();
    public DbSet<CompanyRevision> CompanyRevisions => Set<CompanyRevision>();
    public DbSet<AuditRun> AuditRuns => Set<AuditRun>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentVersion> ContentVersions => Set<ContentVersion>();
    public DbSet<ContentDecision> ContentDecisions => Set<ContentDecision>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.Goal).HasMaxLength(2000);
            entity.Property(x => x.Platform).HasMaxLength(32);
            entity.Property(x => x.Model).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ErrorCode).HasMaxLength(64);
            entity.Property(x => x.Cost).HasPrecision(18, 6);
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ContentItem>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.StateVersion).IsConcurrencyToken();
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ApprovedHash).HasMaxLength(64);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ContentVersion>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.ContentId, x.Version });
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Text).HasMaxLength(5000);
            entity.Property(x => x.ImageBrief).HasMaxLength(2000);
            entity.Property(x => x.AltText).HasMaxLength(500);
            entity.Property(x => x.Target).HasMaxLength(32);
            entity.Property(x => x.TimeZone).HasMaxLength(100);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.ScheduledAt });
            entity.HasOne<ContentItem>().WithMany().HasForeignKey(x => new { x.TenantId, x.ContentId }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ContentDecision>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Decision).HasMaxLength(32);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.HasOne<ContentVersion>().WithMany().HasForeignKey(x => new { x.TenantId, x.ContentId, x.Version }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AuditRun>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Scenario).HasMaxLength(32);
            entity.Property(x => x.RuleVersion).HasMaxLength(32);
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CompanyRecord>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.Kind).HasMaxLength(20);
            entity.Property(x => x.Source).HasMaxLength(2000);
            entity.Property(x => x.DataJson).HasMaxLength(32000);
            entity.HasIndex(x => new { x.TenantId, x.Kind, x.UpdatedAt });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CompanyRevision>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.RecordId, x.Version });
            entity.Property(x => x.Source).HasMaxLength(2000);
            entity.Property(x => x.DataJson).HasMaxLength(32000);
            entity.HasIndex(x => new { x.TenantId, x.RecordId, x.CreatedAt });
            entity.HasOne<CompanyRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecordId }).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Entity<ApplicationUser>().Property(x => x.LastTwoFactorCodeHash).HasMaxLength(64);
        builder.Entity<LoginSession>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Tenant>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Membership>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.UserId });
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.UserId, x.Active, x.TenantId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AuditEvent>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.OccurredAt });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ProtectAuditLog()
    {
        if (ChangeTracker.Entries<ContentVersion>().Any(x => x.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<ContentDecision>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Content history is append-only.");
        if (ChangeTracker.Entries<AuditRun>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit results are append-only.");
        if (ChangeTracker.Entries<CompanyRevision>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Company revisions are append-only.");
        if (ChangeTracker.Entries<AuditEvent>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit events are append-only.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectAuditLog();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectAuditLog();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

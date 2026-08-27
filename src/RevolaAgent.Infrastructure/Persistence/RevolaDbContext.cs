using Microsoft.EntityFrameworkCore;

namespace RevolaAgent.Infrastructure.Persistence;

// No business tables before Phase 2. Migrations are applied explicitly, never on API startup.
public sealed class RevolaDbContext(DbContextOptions<RevolaDbContext> options) : DbContext(options);

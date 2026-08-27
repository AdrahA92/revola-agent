using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RevolaAgent.Infrastructure.Persistence;

public sealed class RevolaDbContextFactory : IDesignTimeDbContextFactory<RevolaDbContext>
{
    public RevolaDbContext CreateDbContext(string[] args)
    {
        // Only schema generation uses the placeholder. Applying migrations requires an explicit connection.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Database=revola_design";
        return new RevolaDbContext(new DbContextOptionsBuilder<RevolaDbContext>().UseNpgsql(connection).Options);
    }
}

namespace RevolaAgent.Infrastructure.Persistence;

public static class DatabaseConfiguration
{
    public static string RequireConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("ConnectionStrings:Database must be configured.");
        }
        return value;
    }
}

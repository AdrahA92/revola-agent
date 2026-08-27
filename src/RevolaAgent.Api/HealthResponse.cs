using Microsoft.Extensions.Diagnostics.HealthChecks;

internal static class HealthResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        // Intentionally omit exceptions, descriptions, server names and connection strings.
        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() }, context.RequestAborted);
    }
}

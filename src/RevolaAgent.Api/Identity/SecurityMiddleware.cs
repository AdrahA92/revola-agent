using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RevolaAgent.Application.Tenancy;

namespace RevolaAgent.Api.Identity;

public static class SecurityMiddleware
{
    public static async Task Invoke(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
            {
                try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
                catch (AntiforgeryValidationException)
                {
                    await Results.Problem(statusCode: 400, title: "CSRF-Prüfung fehlgeschlagen.").ExecuteAsync(context);
                    return;
                }
            }
        }
        try { await next(context); }
        catch (TenancyException exception)
        {
            await Results.Problem(statusCode: exception.Status, title: exception.Message).ExecuteAsync(context);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Conflict(context);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" or "40001" or "40P01" })
        {
            await Conflict(context);
        }
        catch (PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        {
            await Conflict(context);
        }
    }

    private static Task Conflict(HttpContext context) =>
        Results.Problem(statusCode: 409, title: "Daten wurden zwischenzeitlich geändert. Bitte neu laden.").ExecuteAsync(context);
}

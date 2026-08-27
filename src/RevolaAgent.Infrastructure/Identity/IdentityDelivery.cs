using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RevolaAgent.Infrastructure.Identity;

public sealed record IdentityMessage(string Email, Guid UserId, string Purpose, string Token);

public interface IIdentityDelivery
{
    Task SendAsync(IdentityMessage message, CancellationToken ct);
}

// Development-only SMTP capture. No production recipients or configurable external SMTP servers.
public sealed class LocalIdentityDelivery(IHostEnvironment environment, IConfiguration configuration) : IIdentityDelivery
{
    public async Task SendAsync(IdentityMessage message, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Production identity delivery is not configured.");
        var host = configuration["Identity:MailpitHost"] ?? "localhost";
        if (host is not ("localhost" or "127.0.0.1" or "mailpit"))
            throw new InvalidOperationException("Only the local mail capture service is allowed.");
        using var mail = new MailMessage("no-reply@revola.invalid", message.Email)
        {
            Subject = message.Purpose == "confirm" ? "Revola Agent: E-Mail bestätigen" : "Revola Agent: Passwort zurücksetzen",
            Body = $"Lokale Entwicklungsnachricht – kein Produktivversand.\n\nBenutzer-ID: {message.UserId}\nCode: {message.Token}\n\nÖffnen Sie in Ihrer lokalen App die Seite /confirm oder /reset und geben Sie Benutzer-ID und Code ein. Der Code ist eine Stunde gültig.",
            IsBodyHtml = false
        };
        using var smtp = new SmtpClient(host, 1025) { EnableSsl = false, UseDefaultCredentials = false, Timeout = 10000 };
        await smtp.SendMailAsync(mail, ct);
    }
}

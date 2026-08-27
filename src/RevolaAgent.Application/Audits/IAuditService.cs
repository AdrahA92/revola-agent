using RevolaAgent.Application.Company;

namespace RevolaAgent.Application.Audits;

public sealed record DemoAccount(string Scenario, int PostsLast30Days, int ContentFormats, bool HasBrandImage, bool HasContactButton);
public interface IDemoPlatform { DemoAccount Read(string scenario); }
public sealed record Criterion(string Name, int? Score, int Maximum, string Observation, string? Action, string Priority, string Effort, bool RequiresApproval);
public sealed record AuditResult(int Score, string RuleVersion, int AssessedCriteria, int TotalCriteria, bool IsDemo, string Uncertainty, IReadOnlyList<Criterion> Criteria);
public sealed record AuditView(Guid Id, Guid ProfileVersion, string Scenario, DateTime CreatedAt, AuditResult Result);
public interface IAuditService
{
    Task<AuditView> RunAsync(Guid userId, Guid tenantId, Guid id, string scenario, CancellationToken ct);
    Task<IReadOnlyList<AuditView>> ListAsync(Guid userId, Guid tenantId, int page, CancellationToken ct);
}

public static class DemoScoring
{
    public const string Version = "demo-v1";
    public static AuditResult Evaluate(CompanyProfileData profile, DemoAccount account)
    {
        var criteria = new[]
        {
            new Criterion("Profilvollständigkeit", string.IsNullOrWhiteSpace(profile.Description) ? 0 : 10, 10,
                "Beschreibung aus dem gespeicherten Unternehmensprofil.", null, "Niedrig", "–", false),
            new Criterion("Markendarstellung", account.HasBrandImage ? 10 : 0, 10,
                "Profilbild aus dem ausdrücklich fiktiven Demo-Datensatz.", account.HasBrandImage ? null : "Passendes Markenbild vorbereiten.", "Hoch", "30 Minuten", true),
            new Criterion("Kontaktroute", account.HasContactButton ? 10 : 0, 10,
                "Kontaktbutton aus dem fiktiven Demo-Datensatz.", account.HasContactButton ? null : "Kontaktbutton mit geprüfter Website vorbereiten.", "Hoch", "10 Minuten", true),
            new Criterion("Veröffentlichungsaktivität", Math.Min(account.PostsLast30Days, 4) * 10 / 4, 10,
                $"Demo: {account.PostsLast30Days} Beiträge in 30 Tagen; Regelziel mindestens 4.", account.PostsLast30Days >= 4 ? null : "Redaktionsplan mit vier Beiträgen pro 30 Tage entwerfen.", "Mittel", "1 Stunde", false),
            new Criterion("Inhaltsmix", Math.Min(account.ContentFormats, 3) * 10 / 3, 10,
                $"Demo: {account.ContentFormats} Inhaltsformate; Regelziel mindestens 3.", account.ContentFormats >= 3 ? null : "Leistungsbeitrag, Anwendungsszenario und FAQ als Varianten entwerfen.", "Mittel", "1 Stunde", false),
            Unknown("Interaktion und Reaktionszeit"), Unknown("Reichweite und Websiteklicks"), Unknown("Leadpotenzial"),
            Unknown("Technische Verbindungsqualität"), Unknown("Richtlinien und Datenschutz")
        };
        var assessed = criteria.Where(x => x.Score.HasValue).ToArray();
        var score = assessed.Sum(x => x.Score!.Value) * 100 / assessed.Sum(x => x.Maximum);
        return new(score, Version, assessed.Length, criteria.Length, true,
            "Demonstration, keine Bewertung eines verbundenen Kontos. Fünf Kriterien sind nicht messbar und werden nicht als Null gewertet. Keine Aussage zu Reichweite, Leads oder rechtlicher Konformität.", criteria);
    }
    private static Criterion Unknown(string name) => new(name, null, 10, "Keine belastbaren Daten verfügbar.", null, "Nicht bewertet", "–", false);
}

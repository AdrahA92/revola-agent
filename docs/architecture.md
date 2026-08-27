# Architektur

## Status

Dieses Dokument beschreibt die Zielarchitektur. In Phase 0 existiert noch kein Anwendungscode.

## Architekturform

Revola Agent startet als modularer Monolith mit ASP.NET Core, React und PostgreSQL. Diese Form hält Transaktionen und Betrieb einfach, erzwingt aber fachliche Modulgrenzen. Eine spätere Extraktion einzelner Module ist möglich, wenn Last, Verfügbarkeit oder Teamstruktur dies verlangen.

## Systemkontext

- Benutzer bedienen die React-Webanwendung.
- Die Webanwendung verwendet ausschließlich die ASP.NET-Core-API.
- Die API authentifiziert Benutzer, erzwingt Mandant und Berechtigungen und orchestriert Fachmodule.
- PostgreSQL speichert operative Daten und persistente Jobs.
- Der Agent Runtime ruft OpenAI nur mit minimal erforderlichem Kontext auf.
- Plattformadapter kommunizieren ausschließlich mit offiziellen APIs.
- Ein Worker verarbeitet geplante, idempotente Jobs.

## Module

| Modul | Verantwortung |
| --- | --- |
| Identity | Benutzer, Anmeldung, Sitzungen |
| Tenancy | Organisationen, Mitgliedschaften, Rollen, Mandantenkontext |
| CompanyProfile | Unternehmenswissen, Marke, Zielgruppen, Regionen |
| Connections | OAuth, Konten, verschlüsselte Tokens, Capabilities |
| AccountAudit | Snapshots, Regeln, Scores, Empfehlungen |
| Content | Briefings, Inhalte, Versionen, Medien, Kalender |
| Approvals | Freigabeanforderungen und Entscheidungen |
| Publishing | Planung, Jobs, Plattformaufrufe, Status |
| AgentRuntime | Läufe, Schritte, Werkzeuge, Budgets, Modellzugriff |
| Analytics | Metriken, Zeitreihen, Vergleiche, Experimente |
| Leads | Unternehmen, Kontakte, Quellen, Qualifizierung |
| Outreach | Entwürfe, Sperrlisten, Versandstatus |
| Notifications | In-App- und E-Mail-Hinweise |
| AuditLog | unveränderbare sicherheitsrelevante Ereignisse |
| Billing | Tarife und Kontingente nach dem MVP |

## Abhängigkeitsregeln

- Ein Modul legt Verträge über Application Services und Ereignisse offen.
- Kein Modul greift direkt auf Tabellen eines anderen Moduls zu.
- API-Verträge sind von EF-Core-Entitäten getrennt.
- Domänenlogik hängt nicht von HTTP, OpenAI oder konkreten Plattform-SDKs ab.
- Externe Adapter implementieren schmale Ports in den zuständigen Modulen.
- Ereignisse innerhalb des Monolithen werden zunächst persistent in einer Outbox gespeichert.

## Geplante Solution-Struktur

```text
src/
  RevolaAgent.Api/
  RevolaAgent.Application/
  RevolaAgent.Domain/
  RevolaAgent.Infrastructure/
  RevolaAgent.Worker/
web/
  revola-agent-web/
tests/
  RevolaAgent.UnitTests/
  RevolaAgent.IntegrationTests/
  RevolaAgent.ArchitectureTests/
  e2e/
docs/
  adr/
```

Die Modulgrenzen werden innerhalb dieser Projekte durch Namespaces, Verzeichnisse und Architekturtests geschützt. Zusätzliche Projekte pro Modul werden erst eingeführt, wenn dies einen belegten Vorteil bringt.

## Mandantenkontext

Nach Authentifizierung löst die API den aktiven Mandanten aus Benutzer und Mitgliedschaft auf. Eine vom Client gesendete Tenant-ID dient höchstens zur Auswahl und wird gegen die Mitgliedschaft geprüft. Schreib- und Leseoperationen erhalten einen serverseitigen `TenantContext`.

Alle mandantenbezogenen Tabellen besitzen `TenantId`. Datenbankindizes und fachliche Eindeutigkeiten schließen `TenantId` ein. Jobs, Objektpfade, Cache-Schlüssel, Agentenläufe und Auditereignisse tragen ebenfalls den Mandanten.

## Agentenablauf

1. Anwendung erstellt einen `AgentRun` mit Ziel, Budget und erlaubten Werkzeugen.
2. Ein Policy Gate bestimmt Rollen, Risiko und Freigabestatus.
3. Die Runtime baut einen minimalen, redigierten Kontext.
4. OpenAI liefert strukturierte Antworten oder Werkzeuganforderungen.
5. Eingabe und Ausgabe jedes Werkzeugs werden serverseitig validiert.
6. Werkzeugaufrufe werden protokolliert und idempotent ausgeführt.
7. Externe sensible Aktionen werden als Freigabeanforderung angehalten.
8. Das Ergebnis wird gespeichert und dem Benutzer verständlich dargestellt.

## Persistente Jobs

Die erste Version verwendet eine PostgreSQL-Tabelle mit Leasing, Sichtbarkeitszeit, Versuchszähler, nächstem Ausführungszeitpunkt und Idempotency Key. Der Worker beansprucht Jobs atomar. Nach ausgeschöpften Versuchen wechselt ein Job in einen Dead-Letter-Status.

## Datenhaltung

- PostgreSQL: operative und Auditdaten
- Objektspeicher: Medien; im lokalen Betrieb kompatibler Emulator oder Dateiadapter
- verschlüsselter Secret Store: Plattformtokens und API-Secrets
- Vektorsuche erst bei nachgewiesenem Bedarf; Unternehmenswissen startet relational und quellenbasiert

## Schnittstellen

- REST-API mit Problem Details und OpenAPI
- OAuth-Callbacks für Plattformverbindungen
- signaturgeprüfte Webhooks
- interne Outbox-Ereignisse
- keine direkte Browser-zu-Plattform-Kommunikation mit langlebigen Tokens

## Qualitätsmerkmale

- Sicherheit: serverseitige Autorisierung, Freigaben und Datenminimierung
- Nachvollziehbarkeit: versionierte Scores, Inhalte und Agentenläufe
- Zuverlässigkeit: Idempotenz, Outbox, Retries und Dead-Letter-Status
- Änderbarkeit: Adapter und modulare Grenzen
- Kostenkontrolle: Budgets pro Lauf und Mandant
- Bedienbarkeit: verständliche Begründungen statt verborgener Autonomie

## Offene Architekturentscheidungen

- konkreter Identity Provider oder ASP.NET Core Identity
- EU-Hostinganbieter und verwaltete Dienste
- Objektspeicher und Malwareprüfung für Uploads
- erste produktive Plattform und deren Capability-Matrix
- genauer OpenAI-Modellmix und Kostenbudgets

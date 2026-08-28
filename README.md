# Revola Agent

Mandantenfähige SaaS für die spätere Betreuung von Unternehmenskonten durch einen kontrollierten KI-Agenten.

## Aktueller Stand

Grundgerüst, Identity, Mandantenverwaltung, Unternehmenswissen und Demo-Audits sind implementiert. Eine begrenzte Demo-Agent-Runtime erzeugt jetzt Textvorlagen; Content-Versionen, Vier-Augen-Freigaben und interne Terminplanung sind verfügbar. E-Mail-Bestätigung, Passwortwiederherstellung, optionale TOTP-MFA und Sitzungswiderruf sind implementiert; E-Mails gehen ausschließlich an den lokalen Test-Posteingang. Die visuelle Abnahme ist mit Zustimmung des Nutzers zurückgestellt. Echte OpenAI- und Social-Media-Verbindungen fehlen weiterhin. Der Worker veröffentlicht nichts. Dies ist noch keine produktionsreife SaaS.

### Entwürfe, Freigaben und Browser-Fallback

Im Organisationsbereich führen „Content und Freigaben“ und „Social-Media-Konten“ zu den neuen Modulen. Der Demo-Agent verwendet das gespeicherte Unternehmensprofil, keine bezahlte KI. Grenzen: 20 Läufe pro Organisation und UTC-Tag, zwei gleichzeitige Läufe, 15 Sekunden Laufzeit. Bildbeschreibung und Alternativtext sind Textfelder, keine generierten Bilder.

Freigaben gelten für genau eine Version einschließlich Text, Bildbeschreibung, Ziel und Termin. Autoren dürfen eigene Inhalte nicht freigeben. Änderungen machen bisherige Freigaben ungültig; Terminplanung prüft Ablauf und aktuelle Prüfrechte erneut. „Geplant“ bedeutet ausschließlich intern vorgemerkt, nicht bei einer Plattform veröffentlicht.

Der Browser-Fallback öffnet Facebook oder LinkedIn separat; Beitragstexte lassen sich kopieren und manuell einfügen. Eine Anmeldung dort verbindet das Konto **nicht** mit Revola Agent. Es werden weder Browser-Sitzungen noch Plattformpasswörter übernommen. Automatisches Lesen und Veröffentlichen bleiben deaktiviert. Für OAuth fehlen die bestätigte erste Plattform, Berechtigungen, App-Konfiguration und sichere Tokenablage. Details: [ADR 0012](docs/adr/0012-agent-content-and-manual-connections.md).

### Lokale Kontosicherheit

Docker Compose startet Mailpit unter `http://localhost:8025` (SMTP auf localhost:1025). Registrierung erfordert den laufenden Test-Posteingang; es werden keine externen E-Mails versendet. Nach der Registrierung Benutzer-ID und Code aus Mailpit unter `/confirm` eingeben. Bestehende, noch unbestätigte Entwicklungskonten können unter `/resend` eine Bestätigung anfordern. `/forgot` und `/reset` ermöglichen das Zurücksetzen des Passworts. Codes sind eine Stunde gültig. Nach der neuen Migration ist eine erneute Anmeldung notwendig, da alte Cookies keine persistierte Sitzungs-ID besitzen.

Unter `/security` lassen sich eine Authenticator-App manuell einrichten und einzelne Sitzungen beenden. MFA-Aktivierung zeigt zehn einmalige Wiederherstellungscodes und beendet alle Sitzungen. Codes sicher aufbewahren; die Anzeige ist absichtlich flüchtig. Passwortänderungen und -zurücksetzungen beenden ebenfalls alle Sitzungen. Produktivregistrierung und produktiver E-Mail-Versand bleiben gesperrt; persistente verschlüsselte Data-Protection-Schlüssel und ein freigegebener Versanddienst sind noch erforderlich.

### Unternehmenswissen und Demo-Audit

Nach dem Anlegen einer Organisation führen die Links „Unternehmensprofil und Wissen“ und „Demo-Konto-Audit“ zu den neuen Modulen. Unternehmensdaten und zusätzliche Fakten benötigen eine Quelle; Änderungen erzeugen neue Versionen. Owner/Admin/Manager dürfen bearbeiten, andere aktive Mitglieder lesen. Für abweichende Quellen eigene Wissenseinträge anlegen. Logos und Datei-Uploads sind noch nicht implementiert.

Ein gespeichertes Profil ermöglicht ein manuelles Demo-Audit mit zwei fiktiven Szenarien. Der deterministische Score, seine Teilwerte, fehlende Daten und Maßnahmen werden zusammen mit der Historie angezeigt. Das ist keine Bewertung eines echten Social-Media-Kontos. Neue Migrationen vor dem Start anwenden. Technische Einzelheiten und Grenzen: [ADR 0011](docs/adr/0011-company-and-demo-audits.md).

## Voraussetzungen

- .NET SDK 10.0 (siehe `global.json`)
- Node.js 24 und npm
- Docker mit Compose v2 für PostgreSQL und Container-Integrationstests

## Lokal starten

1. Kopiere `.env.example` nach `.env` und trage ein eigenes, ausschließlich lokal verwendetes `POSTGRES_PASSWORD` ein. Verwende für das lokale Beispiel ein alphanumerisches Passwort, damit keine Connection-String-Trennzeichen entstehen.
2. Starte API, Worker und Datenbank:

```sh
docker compose up --build -d
```

3. Wende die Migration ausdrücklich an. Setze vorher `ConnectionStrings__Database` für den lokalen PostgreSQL-Port 5432 mit den Werten aus deiner lokalen Konfiguration (nicht ins Repository schreiben):

```sh
dotnet tool restore
dotnet ef database update --project src/RevolaAgent.Infrastructure --startup-project src/RevolaAgent.Api
```

Ohne Migration bleibt `/health/ready` bei HTTP 503. API und Worker führen niemals automatisch Migrationen aus. Die Design-Time-Factory liest `ConnectionStrings__Database`; API-User-Secrets werden von dieser Factory nicht übernommen. Vor Migration produktiver Daten sind Backup, SQL-Review und ein getrennt berechtigter Migrationsbenutzer erforderlich.

4. Starte in einem zweiten Terminal die Oberfläche:

```sh
cd web/revola-agent-web
npm ci
npm run dev
```

Die Oberfläche läuft unter `http://localhost:5173`, die API unter `http://localhost:5080`. Vite leitet `/health/*` und `/api/*` an die API weiter. Einstieg: `/login`, Registrierung: `/register`, Arbeitsbereich: `/workspace`. Es werden keine Social-Media- oder OpenAI-Zugangsdaten benötigt. Das Development-Setup darf nicht öffentlich bereitgestellt werden.

Die Statusseite macht vor dem Klick auf „Verbindung prüfen“ keine Gesundheitsabfragen. „Bereit“ bei Backend/Datenbank erscheint ausschließlich nach erfolgreicher Antwort. Es gibt keine simulierten Produktmetriken.

## Backend ohne Container ausführen

PostgreSQL muss separat verfügbar sein. Hinterlege die Verbindung in .NET User Secrets für beide Hosts, beispielsweise mit `dotnet user-secrets set "ConnectionStrings:Database" "<lokale Verbindung>" --project src/RevolaAgent.Api`. Wiederhole dies für `src/RevolaAgent.Worker`. Niemals produktive Secrets ins Repository schreiben.

```sh
dotnet restore RevolaAgent.slnx
dotnet run --project src/RevolaAgent.Api -- --urls http://127.0.0.1:5080
# In einem separaten Terminal:
dotnet run --project src/RevolaAgent.Worker
```

Für das OpenAPI-Dokument muss `ASPNETCORE_ENVIRONMENT=Development` gesetzt sein. In anderen Umgebungen ist `/openapi/v1.json` nicht verfügbar.

## Prüfungen

```sh
dotnet build RevolaAgent.slnx -c Release
dotnet test RevolaAgent.slnx -c Release
cd web/revola-agent-web
npm run lint
npm test
npm run build
npx playwright install chromium
npm run test:e2e
```

Ohne Docker können ausschließlich die nicht-containerabhängigen .NET-Tests mit `dotnet test RevolaAgent.slnx --filter 'Category!=Docker'` ausgeführt werden. Das ist keine vollständige Integrationstest-Abnahme. Die CI führt alle Tests einschließlich PostgreSQL-Testcontainers aus.

Die Playwright-Tests prüfen Desktop und Mobilformat mit ausdrücklich gemockten Health-Antworten. Sie ersetzen keinen echten Datenbank- oder Docker-Test.

## Infrastruktur und Sicherheit

- `/health/live` prüft die Erreichbarkeit des Prozesses, unabhängig von PostgreSQL.
- `/health/ready` prüft die tatsächliche PostgreSQL-Verbindung und ausstehende Migrationen; bei Fehlern oder fehlenden Migrationen liefert es HTTP 503.
- Health-Antworten enthalten nur den aggregierten Status, keine Servernamen, Exceptions oder Secrets.
- Strukturierte JSON-Logs und OpenTelemetry-Traces/Metriken sind vorbereitet.
- OTLP-Export ist standardmäßig aus; er wird nur durch `OTEL_EXPORTER_OTLP_ENDPOINT` aktiviert. Vor produktiver Nutzung sind Datenminimierung und Collector-Zugriff zu prüfen.
- Die erste Migration enthält Identity, Organisationen, Mitgliedschaften und Audit-Ereignisse. Die CI prüft Modelldrift und Migrationen auf einer frischen PostgreSQL-Datenbank. Die Anwendung migriert nie automatisch beim Start.
- Die Compose-Konfiguration ist ausschließlich für lokale Entwicklung. Vor Produktion sind TLS, Hosts, Secret Store und Telemetrie-Konfiguration festzulegen.
- Docker-Hosts laufen mit dem nicht privilegierten .NET-App-Benutzer.

## Projektunterlagen

- [Phase-2-API und Grenzen](docs/identity-tenancy-api.md)

- [Produktumfang](docs/product-scope.md)
- [Architektur](docs/architecture.md)
- [Sicherheit](docs/security.md)
- [Agentenrichtlinien](docs/agent-policy.md)
- [Roadmap](docs/development-roadmap.md)

Die verbindlichen Entwicklungsregeln stehen in [AGENTS.md](AGENTS.md).

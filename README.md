# Revola Agent

Mandantenfähige SaaS für die spätere Betreuung von Unternehmenskonten durch einen kontrollierten KI-Agenten.

## Aktueller Stand

Phase 1 stellt das technische Grundgerüst bereit. Phase 2 ergänzt ASP.NET Core Identity, Cookie-Anmeldung, Organisationen, Mitgliedschaften, Rollen, CSRF-Schutz und Auditierung. Die React-Oberfläche enthält Anmeldung, Entwicklungsregistrierung, Organisationsübersicht, Einladungen, Mitgliederverwaltung und Auditansicht. E-Mail-Bestätigung, Passwortwiederherstellung, MFA und die visuelle Abnahme sind noch offen. Agenten und Social-Media-Verbindungen sind noch nicht implementiert. Der Worker führt noch keine Aufgaben aus. Dies ist noch keine produktionsreife SaaS.

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

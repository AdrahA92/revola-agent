# Revola Agent

Mandantenfähige SaaS für die spätere Betreuung von Unternehmenskonten durch einen kontrollierten KI-Agenten.

## Aktueller Stand

Phase 1 stellt das technische Grundgerüst bereit: ASP.NET-Core-API, Worker-Host, React/Vite-Statusseite, PostgreSQL-Konfiguration, Tests und CI. Anmeldung, Mandantendaten, Agenten und Social-Media-Verbindungen sind noch nicht implementiert. Der Worker führt noch keine Aufgaben aus.

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

3. Starte in einem zweiten Terminal die Oberfläche:

```sh
cd web/revola-agent-web
npm ci
npm run dev
```

Die Oberfläche läuft unter `http://localhost:5173`, die API unter `http://localhost:5080`. Vite leitet `/health/*` an die API weiter. Es werden keine Social-Media- oder OpenAI-Zugangsdaten benötigt.

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
- `/health/ready` prüft die tatsächliche PostgreSQL-Verbindung und liefert bei Fehlern HTTP 503.
- Health-Antworten enthalten nur den aggregierten Status, keine Servernamen, Exceptions oder Secrets.
- Strukturierte JSON-Logs und OpenTelemetry-Traces/Metriken sind vorbereitet.
- OTLP-Export ist standardmäßig aus; er wird nur durch `OTEL_EXPORTER_OTLP_ENDPOINT` aktiviert. Vor produktiver Nutzung sind Datenminimierung und Collector-Zugriff zu prüfen.
- Noch keine Fachschema-Migrationen: Phase 1 enthält einen leeren DbContext. Schema und Migrations-Gate folgen mit Phase 2. Die Anwendung migriert nie automatisch beim Start.
- Die Compose-Konfiguration ist ausschließlich für lokale Entwicklung. Vor Produktion sind TLS, Hosts, Secret Store und Telemetrie-Konfiguration festzulegen.
- Docker-Hosts laufen mit dem nicht privilegierten .NET-App-Benutzer.

## Projektunterlagen

- [Produktumfang](docs/product-scope.md)
- [Architektur](docs/architecture.md)
- [Sicherheit](docs/security.md)
- [Agentenrichtlinien](docs/agent-policy.md)
- [Roadmap](docs/development-roadmap.md)

Die verbindlichen Entwicklungsregeln stehen in [AGENTS.md](AGENTS.md).

# Entwicklungsroadmap

## Statusübersicht

| Phase | Inhalt | Status |
| --- | --- | --- |
| 0 | Produkt- und Architekturgrundlage | Abgeschlossen – Bestätigung ausstehend |
| 1 | Projektgrundgerüst | Nicht begonnen |
| 2 | Identität und Mandantenfähigkeit | Nicht begonnen |
| 3 | Unternehmensprofil und Wissensbasis | Nicht begonnen |
| 4 | Demo-Plattform und Konto-Audit | Nicht begonnen |
| 5 | Agent Runtime | Nicht begonnen |
| 6 | Content und Freigaben | Nicht begonnen |
| 7 | erste echte Plattformintegration | Blockiert bis Bestätigung |
| 8 | Analytics und Begleitung | Nicht begonnen |
| 9 | Leads und CRM | Nicht begonnen |
| 10 | Outreach | Nicht begonnen |
| 11 | Abrechnung und Produktreife | Nicht begonnen |

## Priorisierter Backlog

### P0 – Fundament

- [x] Produktziel, Zielgruppen, MVP und Nicht-Ziele definieren
- [x] Rollen und Kernabläufe definieren
- [x] Modul- und Zielarchitektur festlegen
- [x] Bedrohungsmodell und Freigaberegeln dokumentieren
- [x] Agenten- und Integrationsrichtlinien definieren
- [x] erste ADRs erstellen
- [ ] offene Phase-0-Entscheidungen durch Product Owner bestätigen

### P1 – Lauffähige Entwicklungsumgebung

- [ ] .NET-Solution und React-App erzeugen
- [ ] PostgreSQL über Docker Compose bereitstellen
- [ ] Unit-, Integrations-, Architektur- und Frontendtests einrichten
- [ ] CI für Build, Tests, Lint und Secret Scan erstellen
- [ ] Logging, Health Checks und OpenTelemetry-Grundlage einrichten

### P2 – Sicherer Mandantenkern

- [ ] Identity-Entscheidung bestätigen
- [ ] Organisation, Mitgliedschaft und Rollen implementieren
- [ ] TenantContext und Autorisierung implementieren
- [ ] Isolationstests für alle Zugriffsarten erstellen
- [ ] AuditLog-Grundlage implementieren

### P3 – Unternehmenswissen

- [ ] Onboarding und CompanyProfile
- [ ] Marke, Leistungen, Zielgruppen und Regionen
- [ ] quellenbasierte Wissenseinträge und Versionierung
- [ ] Datei-/Logo-Konzept bestätigen

### P4 – Kernnutzen beweisen

- [ ] Fake-Plattformadapter
- [ ] versionierte Auditregeln
- [ ] Score, Teilwerte und Empfehlungen
- [ ] Audit-Historie und Vergleich

### P5/P6 – KI und Content

- [ ] OpenAI-Anbindung mit Test-Double
- [ ] AgentRun, Toolkatalog, Policy Gate und Budgets
- [ ] Briefing, ContentVersion und Medien
- [ ] Freigaben und Demo-Publishing

## Phase-0-Akzeptanzkriterien

- Produktumfang und Nicht-Ziele sind widerspruchsfrei dokumentiert.
- Kernabläufe besitzen klare menschliche Kontrollpunkte.
- Mandantentrennung und Freigaben sind Architekturanforderungen.
- Audit-Scoring ist deterministisch; KI erklärt statt bewertet frei.
- Produktionsintegrationen verwenden ausschließlich offizielle APIs.
- Risiken und Schutzmaßnahmen sind nachvollziehbar.
- schwer reversible Entscheidungen sind als ADR festgehalten.
- offene Entscheidungen sind sichtbar und blockieren die richtige spätere Phase.

## Definition of Done für Implementierungsphasen

- Akzeptanzkriterien erfüllt
- Build, Lint und relevante Tests erfolgreich
- Mandant und Autorisierung geprüft
- Fehlerfälle und Idempotenz behandelt
- keine Secrets oder echten personenbezogenen Testdaten
- Migrationsprüfung auf frischer Datenbank erfolgreich
- Dokumentation entspricht dem tatsächlich implementierten Stand
- keine deaktivierten Tests oder ungeklärten Warnungen

## Offene Entscheidungen vor Phase 1

1. Produktname: vorläufig `Revola Agent`
2. Identity: ASP.NET Core Identity im eigenen Backend oder externer Provider
3. UI-Komponentenbibliothek
4. lokaler Objektspeicher bereits in Phase 1 oder erst Phase 3
5. Hostingziel für Staging und Produktion

## Empfohlener nächster Auftrag

Nach Bestätigung von Phase 0:

> Setze ausschließlich Phase 1 gemäß AGENTS.md und docs/development-roadmap.md um. Erzeuge das .NET-10-/React-Projektgrundgerüst, Docker Compose mit PostgreSQL, Tests, CI, Health Checks, strukturiertes Logging und OpenTelemetry. Implementiere noch keine fachlichen Module, Authentifizierung, OpenAI- oder Plattformintegration.

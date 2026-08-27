# Entwicklungsroadmap

## Statusübersicht

| Phase | Inhalt | Status |
| --- | --- | --- |
| 0 | Produkt- und Architekturgrundlage | Abgeschlossen; Phase 1 am 27.08.2026 beauftragt |
| 1 | Projektgrundgerüst | Implementiert, Verifikation in Bearbeitung |
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

- [x] .NET-Solution und React-App erzeugen
- [x] PostgreSQL über Docker Compose konfigurieren
- [x] Unit-, Integrations-, Architektur- und Frontendtests einrichten
- [x] CI für Build, Tests, Lint und Secret Scan erstellen
- [x] Logging, Health Checks und OpenTelemetry-Grundlage einrichten
- [ ] Docker-Start, PostgreSQL-Integrationstest und CI-Lauf erfolgreich verifizieren
- [ ] Browserprüfung für Desktop und Mobilformat abschließen

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

## Zur zuständigen Phase zurückgestellte Entscheidungen

1. Produktname: vorläufig `Revola Agent`
2. Identity: ASP.NET Core Identity im eigenen Backend oder externer Provider – vor Phase 2
3. UI-Komponentenbibliothek – vor dem ersten fachlichen UI-Modul
4. lokaler Objektspeicher – vor Phase 3
5. Hostingziel für Staging und Produktion – vor Deployment

Siehe ADR 0007 für die bewusst begrenzten Entscheidungen des Grundgerüsts.

## Empfohlener nächster Auftrag

Verifikation am 27.08.2026: .NET-Release-Build ohne Warnungen, neun nicht-containerabhängige .NET-Tests, Frontend-Lint, drei React-Tests und Vite-Produktionsbuild erfolgreich. Docker ist in der Arbeitsumgebung nicht installiert; der Browserzugriff auf die lokale Vorschau wurde blockiert. Container-, E2E- und visuelle Abnahme sowie der vollständige CI-Lauf sind daher noch offen. Phase 1 ist implementiert, aber noch nicht vollständig abgenommen.

Nach vollständiger Verifikation von Phase 1 und Bestätigung der Identity-Entscheidung:

> Setze ausschließlich Phase 2 gemäß AGENTS.md um: Identität, Organisationen, Mitgliedschaften, Rollen, TenantContext, Autorisierung und Isolationstests. Implementiere noch keine OpenAI- oder Plattformintegration.

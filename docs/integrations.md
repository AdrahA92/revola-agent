# Integrationen und Capability-Modell

## Status

Bis einschließlich Phase 6 werden ausschließlich Demo-Daten und lokale Textvorlagen verwendet. Die Kontenübersicht bietet Facebook und LinkedIn als manuelle Browser-Arbeitsweisen an, nicht als verbundene Konten. Kopieren und Öffnen lösen keine Veröffentlichung aus. Eine produktive Plattform wird erst nach API- und Richtlinienprüfung sowie Bestätigung der Berechtigungen ausgewählt. Siehe ADR 0012.

## Grundregeln

- offizielle API und OAuth statt gespeicherter Passwörter
- minimale Scopes
- serverseitig verschlüsselte Tokens
- Capability-Erkennung pro verbundenem Konto
- Rate-Limit- und Retry-After-Unterstützung
- signaturgeprüfte Webhooks
- idempotente externe Änderungen
- keine Behauptung nicht unterstützter Funktionen

## Capability-Katalog

Ein Adapter kann unabhängig folgende Fähigkeiten melden:

- `ReadProfile`
- `ReadContactInfo`
- `ReadPosts`
- `ReadMetrics`
- `UpdateProfile`
- `CreateDraft`
- `PublishPost`
- `SchedulePost`
- `UploadMedia`
- `ReadComments`
- `ReplyToComment`
- `ReadMessages`
- `SendMessage`

Die UI und der Agent dürfen nur gemeldete Fähigkeiten anbieten. Eine Capability enthält zusätzlich benötigte Scopes, Kontotypen, bekannte Limits und Risikoklasse.

## Fake-Adapter

Der geplante vollständige Fake-Adapter soll simulieren (derzeit existieren nur zwei feste Audit-Szenarien):

- vollständige und unvollständige Profile
- Beiträge und Kennzahlen
- fehlende Berechtigungen
- Tokenablauf
- Rate Limits
- temporäre Fehler
- erfolgreiche und doppelte Veröffentlichung

Er erzeugt niemals echte externe Nebenwirkungen und ist Standard für automatisierte Tests.

## Auswahlkriterien für die erste Plattform

- stabile offizielle API
- Unterstützung von Unternehmensprofilen
- zulässiger Lesezugriff für Auditdaten
- zulässige Veröffentlichung nach Freigabe
- dokumentierte OAuth- und Review-Prozesse
- vertretbare Entwicklungs- und Betriebskosten
- Eignung für die Zielkunden

## Vor Einführung erforderliche Nachweise

- Capability-Matrix mit Quellen und Prüfdatum
- genehmigte OAuth-Scopes
- Datenschutz- und Auftragsverarbeitungsprüfung
- Sandbox- oder Testkonto
- Webhook- und Rate-Limit-Tests
- dokumentierte Deaktivierung und Tokenlöschung
- Freigabe durch Product Owner

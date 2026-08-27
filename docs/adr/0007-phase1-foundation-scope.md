# ADR 0007: Phase-1-Grundgerüst ohne vorgezogene Fachentscheidungen

- Status: Angenommen für das technische Grundgerüst
- Datum: 2026-08-27

## Kontext

Der Product Owner hat mit „Weiter mit der nächsten Phase“ Phase 1 beauftragt. Identity Provider, Hosting und UI-Komponentenbibliothek sind noch nicht entschieden. Diese Entscheidungen sind für den technischen Start nicht erforderlich.

## Entscheidung

- Produktname bleibt vorläufig Revola Agent.
- Authentifizierung und Identity-Entscheidung werden erst in Phase 2 umgesetzt.
- Die Statusseite verwendet ausschließlich native HTML-Elemente mit kleinen CSS-Tokens; keine Komponentenbibliothek wird vorweggenommen.
- Medien und Objektspeicher werden erst in Phase 3 benötigt.
- API und Worker verwenden dieselbe Infrastructure-Basis für EF Core, Logging und Telemetrie.
- Die API bietet nur Liveness, Readiness und OpenAPI im Development-Modus.
- Ein leerer DbContext erzeugt keine Fachschema-Migration. Der echte PostgreSQL-Verbindungstest ist Bestandteil der CI; Migrationsprüfungen werden mit der ersten Migration verbindlich ergänzt.
- Docker Compose bindet veröffentlichte Backend-/Datenbankports nur an die lokale Loopback-Adresse.
- Es gibt keinen automatischen OTLP-Export ohne konfigurierte Zieladresse.

## Konsequenzen

Phase 1 bleibt lokal ausführbar, ohne Konten anzulegen, externe Daten zu übertragen oder Geschäftslogik zu simulieren. Eine spätere Identity-/Hostingwahl bleibt offen. Die Statusseite ist ein technischer Einstieg, keine vorgetäuschte fertige SaaS.

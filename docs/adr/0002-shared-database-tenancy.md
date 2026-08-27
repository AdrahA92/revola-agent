# ADR 0002: Gemeinsame Datenbank mit TenantId

- Status: Angenommen für MVP
- Datum: 2026-08-26

## Kontext

Eine Datenbank pro Mandant erhöht Isolation, aber auch Provisionierungs-, Migrations- und Betriebskosten.

## Entscheidung

Das MVP verwendet eine gemeinsame PostgreSQL-Datenbank und gemeinsame Tabellen mit verpflichtender `TenantId`. Serverseitiger TenantContext, Autorisierung, Indizes und Isolationstests sichern die Trennung.

## Konsequenzen

- kostengünstiger und einfacher Betrieb
- jeder Datenzugriff ist sicherheitskritisch
- Architektur- und Integrationstests müssen Cross-Tenant-Zugriffe verhindern
- Enterprise-Isolationsoptionen können später ergänzt werden

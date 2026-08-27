# ADR 0001: Modularer Monolith

- Status: Angenommen
- Datum: 2026-08-26

## Kontext

Das Produkt umfasst viele Fachbereiche, startet aber mit einem kleinen Team und einem noch zu validierenden Markt.

## Entscheidung

Backend und Worker werden als modularer Monolith entwickelt. Module besitzen klare Verträge und geschützte Datenzugriffe. Eine Microservice-Extraktion erfolgt nur bei belegtem Bedarf.

## Konsequenzen

- einfache lokale Entwicklung und Transaktionen
- geringere Betriebs- und Observability-Komplexität
- Architekturtests müssen Modulgrenzen schützen
- spätere Extraktion verlangt bewusst entworfene Verträge

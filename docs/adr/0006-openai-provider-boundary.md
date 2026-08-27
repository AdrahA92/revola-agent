# ADR 0006: Schmale OpenAI-Anbietergrenze

- Status: Angenommen
- Datum: 2026-08-26

## Kontext

Die SaaS soll OpenAI-Agentenfunktionen nutzen, ohne Domänenlogik an konkrete SDK-Typen zu koppeln oder eine unnötige Universalabstraktion zu bauen.

## Entscheidung

Die Application-Schicht definiert schmale Anwendungsports für strukturierte Generierung und Agentenläufe. Die Infrastructure-Schicht implementiert sie mit der aktuellen offiziellen OpenAI-API. Anbieterantworten werden in eigene Verträge übersetzt.

## Konsequenzen

- SDK-Updates bleiben überwiegend in der Infrastruktur
- Tests können kostenfreie Doubles verwenden
- anbieterspezifische Fähigkeiten dürfen gezielt sichtbar bleiben

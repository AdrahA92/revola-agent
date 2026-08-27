# ADR 0005: Deterministisches Audit-Scoring

- Status: Angenommen
- Datum: 2026-08-26

## Kontext

Ein frei vom Sprachmodell erzeugter Score wäre nicht reproduzierbar und könnte Kunden irreführen.

## Entscheidung

Scores werden durch versionierte, deterministische Regeln aus verfügbaren Daten berechnet. Das Sprachmodell erklärt Ergebnisse und formuliert Maßnahmen, verändert aber weder Regelwert noch Gewichtung.

## Konsequenzen

- reproduzierbare und testbare Bewertungen
- Regeln, Gewichte und Datengrundlagen müssen versioniert werden
- fehlende Daten und Unsicherheit müssen sichtbar sein

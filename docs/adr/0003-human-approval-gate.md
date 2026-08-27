# ADR 0003: Serverseitiges Freigabe-Gate

- Status: Angenommen
- Datum: 2026-08-26

## Kontext

Agenten können überzeugende, aber falsche oder unerwünschte Aktionen vorschlagen. Öffentliche Kommunikation kann wirtschaftliche und rechtliche Folgen haben.

## Entscheidung

Externe sensible, finanzielle und destruktive Aktionen benötigen eine serverseitig geprüfte, inhalts- und zielgebundene Freigabe. Das Modell kann keine Freigabe erteilen oder die Risikoklasse ändern.

## Konsequenzen

- höhere Sicherheit und Nachvollziehbarkeit
- zusätzlicher Freigabeschritt in der Bedienung
- Versionierung und Hashing der freigegebenen Aktion erforderlich

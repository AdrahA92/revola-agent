# ADR 0011: Unternehmenswissen und Demo-Audits

Status: Implementierter Entwicklungskern, keine externe Plattformanbindung.

## Unternehmenswissen

Ein typisiertes Unternehmensprofil pro Mandant enthält Kontakt, Leistungen, Zielgruppen, Regionen, Markenfarbe, Schreibstil, Aussagenregeln und Ziele. Weitere Wissenseinträge enthalten Titel, Text, Quelle und optionalen UTC-Ablaufzeitpunkt. Es werden keine URLs abgerufen und keine Dateien hochgeladen. Eine gemeinsame Quelle gilt für die jeweilige gespeicherte Profilversion; Fakten mit abweichender Herkunft gehören in eigene Wissenseinträge. Daten werden niemals aus Modellantworten automatisch übernommen.

Die typisierten DTOs werden als begrenzte JSON-Nutzdaten gespeichert, mit separaten Index-/Versions-/Quellenfeldern. Rollen Owner, Admin und Manager dürfen schreiben; aktive Mitglieder dürfen lesen. Jeder Zugriff prüft die Mitgliedschaft. Zusammengesetzte Schlüssel und Fremdschlüssel binden Historieneinträge an ihren Mandanten. Serialisierbare Transaktionen, Concurrency-Versionen und clientseitige neue Versions-IDs verhindern verlorene Änderungen und duplizierte Wiederholungen. Historie und Sicherheitsaudit werden atomar gespeichert. PostgreSQL-Trigger verhindern Änderungen an der Historie, auch bei direktem SQL.

## Demo-Audits

Der Fake-Adapter kennt nur `starter` und `active`. Snapshot und Profilversion werden mit Regelversion `demo-v1` unveränderlich gespeichert. Es gibt keine OAuth-Tokens, keine API-Aufrufe und keine angeblich echten Kennzahlen. Manuelle Läufe benötigen Owner/Admin/Manager; Wiederholungen derselben Lauf-ID liefern dasselbe Ergebnis.

Fünf gleichgewichtete Kriterien sind berechenbar: Profilbeschreibung, Demo-Markenbild, Demo-Kontaktbutton, Beiträge in 30 Tagen (Ziel 4), Inhaltsformate (Ziel 3). Teilwerte werden auf ganze Punkte abgerundet. Der Score ist die Summe geteilt durch 50, mal 100. Daher erzielt `starter` mit 10+0+0+5+3 Punkten 36/100; `active` 100/100. Diese Zielwerte sind Demonstrationsregeln, keine wissenschaftlich bestätigten Erfolgsfaktoren.

Interaktion, Reichweite, Leads, technische Verbindung und rechtliche Risiken bleiben ausdrücklich unbewertet. Fehlende Werte fließen nicht als Null ein. Empfehlungen enthalten Beobachtung, Maßnahme, Priorität, Aufwand und Freigabehinweis. Annahme/Ablehnung und spätere Erfolgskontrolle gehören zur noch ausstehenden kontinuierlichen Begleitung.

## Grenzen

Noch offen: Logo-/Dateispeicher einschließlich Freigabe der Betriebsregion, Upload-Prüfung und Löschfristen; strukturierte Mehrfach-Marken/-Zielgruppenverwaltung; visuelle Abnahme. Keine Produktionsempfehlung. OpenAI ist noch nicht angebunden und ein API-Schlüssel ist in dieser Umgebung nicht konfiguriert. Modell, Kostenobergrenze und zulässige Datenübertragung müssen vor echten Modellaufrufen festgelegt werden.

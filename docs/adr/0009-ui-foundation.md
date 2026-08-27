# ADR 0009: React-Verwaltungsoberfläche und aufeinanderfolgende Phasen

- Datum: 2026-08-27
- Status: Implementiert, visuelle Abnahme blockiert

Der Product Owner beauftragt alle Phasen in Reihenfolge. Damit entfällt die erneute Beauftragung jedes Meilensteins, nicht aber dessen Qualitätsprüfung oder die erforderliche Freigabe externer Plattformen, Kosten und Berechtigungen.

Als austauschbare Komponentenbasis wird MUI verwendet. React Hook Form und Zod validieren Formulare. Der Entwurf führt die vorhandenen Farben fort: Weiß, Navy und Teal, offene Listen statt Kartenraster, klare Formularbeschriftungen und responsive Umbrüche. Authentifizierte Ansichten erweitern dieselben Gestaltungsregeln für die tatsächlich notwendigen Verwaltungsabläufe. Es gibt keine simulierten Produktmetriken oder statischen Bilder als Bedienoberfläche.

Die API setzt HttpOnly-Cookies; der Browser erhält einen CSRF-Token für jeden Schreibvorgang. Tokens und Passwörter werden nicht in LocalStorage gespeichert. Abfragen verwenden Benutzer-/Mandanten-IDs in ihren Cache-Schlüsseln. Kontoübergreifende Cache-Inhalte werden nach Anmeldung und Abmeldung verworfen. Mutationen werden nicht automatisch wiederholt. Versionskonflikte laden aktuelle Mitgliedschaften nach. Entfernen einer Mitgliedschaft benötigt eine explizite Bestätigung im Dialog.

Umgesetzt: Anmeldung, Registrierung im Development-Modus, Organisationsanlage/-auswahl, Annahme von Einladungen, Rollenwechsel, Entfernen von Mitgliedschaften und Auditansicht. Fehlende Rechte werden angezeigt; die tatsächliche Autorisierung bleibt im Backend. Lazy Loading trennt Authentifizierungs- und Verwaltungsseiten.

Die Browserumgebung verweigert die lokale Vorschau mit `ERR_BLOCKED_BY_CLIENT`. Deshalb liegen keine vergleichbaren Browser-Screenshots vor; Schrift, Layout, Farben, Abstände und mobile Darstellung sind noch nicht visuell abgenommen. Automatisierte Tests ersetzen diesen Vergleich nicht. Keine Aussage über Produktionsreife oder abgeschlossene Phasen 3–11.

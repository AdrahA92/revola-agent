# Identity- und Mandanten-API – Phase 2, Backend-Teilschritt

## Entwicklungsablauf

1. PostgreSQL starten und die Migration ausdrücklich anwenden (README).
2. `GET /api/identity/csrf` aufrufen. Das Cookie speichern und den JSON-Wert `token` bei jedem schreibenden Request als `X-CSRF-TOKEN` senden.
3. In Development mit `POST /api/identity/register` und `{ "email": "user@example.test", "password": "<eigenes Passwort>" }` registrieren. Passwort: 12–128 Zeichen, Groß-/Kleinbuchstaben, Zahl und Sonderzeichen.
4. Mit denselben Feldern an `POST /api/identity/login` anmelden. Cookies bei Folgeaufrufen mitsenden. Nach jedem Identitätswechsel einen neuen CSRF-Token abholen. Keine Tokens in LocalStorage speichern.
5. `GET /api/identity/me` liefert die eigene Benutzer-ID; diese kann ein Benutzer gezielt für eine Einladung weitergeben.
6. Organisation mit `PUT /api/tenants/{neue UUID}` und `{ "name": "Beispielunternehmen" }` anlegen. Dieselbe UUID und derselbe Inhalt können sicher erneut gesendet werden.

Die UI unterstützt diese Schritte noch nicht. Der Vite-Proxy bedient derzeit ausschließlich die technische Statusseite. Für manuelle API-Tests ist direkt die API-Adresse zu verwenden; eine vollständige Browserabnahme ist noch offen.

## Endpunkte

Alle Pfade beginnen mit `/api`. Außer CSRF, Registrierung und Login benötigen alle Endpunkte eine gültige Sitzung. Alle mutierenden Requests benötigen CSRF, auch Login und Registrierung.

| Methode / Pfad | Inhalt / Wirkung |
| --- | --- |
| GET `identity/me` | eigene Benutzer-ID |
| POST `identity/logout` | alle eigenen Sitzungen widerrufen |
| POST `identity/password` | `currentPassword`, `newPassword`; danach erneut anmelden |
| GET `tenants?page=1` | eigene aktive Organisationen, 50 je Seite |
| PUT `tenants/{tenantId}` | `name`; Anlage mit Owner-Mitgliedschaft |
| GET `tenants/{tenantId}/members?page=1` | Mitglieder und offene Einladungen; Owner/Admin |
| PUT `tenants/{tenantId}/members/{userId}/invitation` | `role`; bestehender registrierter Benutzer, zunächst inaktiv |
| GET `invitations?page=1` | ausschließlich eigene offene Einladungen |
| PUT `invitations/{tenantId}/accept` | `version` der eigenen Einladung |
| PUT `tenants/{tenantId}/members/{userId}/role` | `role`, `version`; Berechtigung und Nebenläufigkeit geprüft |
| DELETE `tenants/{tenantId}/members/{userId}?version={version}` | Mitgliedschaft/Einladung entfernen; Owner geschützt |
| GET `tenants/{tenantId}/audit?page=1` | mandantenbezogene Auditereignisse; Owner/Admin |

Rollen sind exakt `Owner`, `Admin`, `Manager`, `Editor`, `Approver`, `Viewer`. Owner kann nicht über die Einladungs-/Rollenendpunkte vergeben werden. Die `version` stammt aus der aktuellen Mitgliedschaft oder Einladung. Nach 409 neu laden; eine veraltete Änderung niemals blind überschreiben. Antworten sind DTOs ohne Passwort-Hashes, E-Mail-Adressen oder Security Stamps.

## Sicherheitsgrenzen

- Einladungen geben vor Annahme keinerlei Mandantenzugriff. Kein E-Mail-Versand, kein öffentlicher Benutzer-Suchendpunkt.
- Fremde Mandanten liefern bei Zugriffen 404. Fehlende Rechte innerhalb der eigenen Organisation liefern 403. Kontoanfragen verwenden generische Fehlermeldungen.
- Auditierung enthält Akteur-/Ziel-IDs, Aktion und UTC-Zeitpunkt. Keine Passwörter, E-Mails oder Request-Payloads. Tenant-Auditabfragen schließen globale Identity-Ereignisse aus.
- Sitzungen enden spätestens nach 30 Minuten. Security Stamps werden bei jedem authentifizierten Request geprüft. Logout und Passwortänderung widerrufen alle vorhandenen Sitzungen.
- Nach fünf falschen Passwörtern gilt eine 15-minütige Kontosperre. Identity-Schreibendpunkte erlauben 20 Requests pro Minute/IP; Mandantenendpunkte 120 Requests pro Minute/Benutzer. Zähler gelten pro Prozess.
- In Production bleibt die Registrierung deaktiviert und Login erfordert bestätigte E-Mail. E-Mail-Verifikation, Recovery, MFA, Sitzungsübersicht, Frontend und produktive Schlüsselverwaltung sind noch nicht fertig. Nicht mit Development-Einstellungen öffentlich bereitstellen.

## Verifikation

Lokale HTTP-Integrationstests verwenden SQLite, echte Identity-Cookies und echten CSRF-Schutz; die Authentifizierung wird nicht gemockt. Separat prüft ein PostgreSQL-Testcontainer frische/wiederholte Migration, Modelldrift, Readiness, Identity-Anmeldung, fremden Mandantenzugriff und SQL-Auditmanipulation. Docker ist in dieser Arbeitsumgebung nicht verfügbar; dieser Test muss in CI/Entwicklung noch erfolgreich ausgeführt werden.

Siehe [ADR 0008](adr/0008-aspnet-identity.md) für Entscheidungen und Grenzen.

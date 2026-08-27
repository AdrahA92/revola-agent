# Identity- und Mandanten-API – Phase 2, Backend-Teilschritt

## Entwicklungsablauf

1. PostgreSQL starten und die Migration ausdrücklich anwenden (README).
2. `GET /api/identity/csrf` aufrufen. Das Cookie speichern und den JSON-Wert `token` bei jedem schreibenden Request als `X-CSRF-TOKEN` senden.
3. In Development mit `POST /api/identity/register` und `{ "email": "user@example.test", "password": "<eigenes Passwort>" }` registrieren. Passwort: 12–128 Zeichen, Groß-/Kleinbuchstaben, Zahl und Sonderzeichen.
4. Mailpit starten, den Bestätigungscode aus dem lokalen Posteingang mit `POST /api/identity/confirm-email` (`userId`, `token`) einlösen. Danach mit denselben Zugangsdaten an `POST /api/identity/login` anmelden. Bei HTTP 202 mit `requiresMfa: true` entsteht noch keine Sitzung: Zugangsdaten erneut mit `code` oder `recoveryCode` senden. Cookies bei Folgeaufrufen mitsenden. Nach jedem Identitätswechsel einen neuen CSRF-Token abholen. Keine Tokens in LocalStorage speichern.
5. `GET /api/identity/me` liefert die eigene Benutzer-ID; diese kann ein Benutzer gezielt für eine Einladung weitergeben.
6. Organisation mit `PUT /api/tenants/{neue UUID}` und `{ "name": "Beispielunternehmen" }` anlegen. Dieselbe UUID und derselbe Inhalt können sicher erneut gesendet werden.

Die UI unterstützt Anmeldung und Entwicklungsregistrierung unter `/login` und `/register`. Unter `/workspace` können Organisationen angelegt und Einladungen angenommen werden; `/workspace/{tenantId}` enthält die berechtigungsgesteuerte Mitgliederverwaltung und Auditansicht. Der Vite-Proxy leitet `/api` an das Backend weiter. Die vollständige visuelle Browserabnahme ist noch offen.

## Endpunkte

Alle Pfade beginnen mit `/api`. CSRF, Registrierung, Login, Bestätigung und Passwort-Recovery sind ohne Sitzung erreichbar. Alle übrigen Endpunkte benötigen eine gültige Sitzung. Alle mutierenden Requests benötigen CSRF, auch Login und Registrierung.

| Methode / Pfad | Inhalt / Wirkung |
| --- | --- |
| GET `identity/me` | eigene Benutzer-ID |
| POST `identity/request-confirmation` | `email`; generisches 202, nur lokaler Testversand |
| POST `identity/confirm-email` | `userId`, `token`; E-Mail bestätigen |
| POST `identity/request-reset` | `email`; generisches 202, nur lokaler Testversand |
| POST `identity/reset-password` | `userId`, `token`, `newPassword`; einmalig, beendet Sitzungen |
| GET `identity/sessions?page=1` | eigene aktive Sitzungen, 50 je Seite |
| DELETE `identity/sessions/{id}` | eigene Sitzung widerrufen |
| GET `identity/mfa/status` | Aktivierungsstatus und verbleibende Recovery-Codes |
| POST `identity/mfa/setup` | `password`; manueller Authenticator-Schlüssel, nur solange MFA nicht aktiv |
| POST `identity/mfa/enable` | `password`, `code`; zehn einmalig ausgegebene Recovery-Codes, beendet Sitzungen |
| POST `identity/mfa/disable` | `password` und `code` oder `recoveryCode`; beendet Sitzungen |
| POST `identity/logout` | alle eigenen Sitzungen widerrufen |
| POST `identity/password` | `currentPassword`, `newPassword`; danach erneut anmelden |
| GET `tenants?page=1` | eigene aktive Organisationen, 50 je Seite |
| GET `tenants/{tenantId}` | Name und eigene Rolle nach Prüfung aktiver Mitgliedschaft |
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
- Login erfordert in jeder Umgebung eine bestätigte E-Mail. In Production bleiben Registrierung und Zustellung deaktiviert. Bestätigung, Recovery, optionale MFA und Sitzungsübersicht sind implementiert; produktive Schlüssel-/Secret-Verschlüsselung, Zustellintegration und MFA-Pflicht stehen aus. Nicht mit Development-Einstellungen öffentlich bereitstellen.

## Verifikation

Lokale HTTP-Integrationstests verwenden SQLite, echte Identity-Cookies und echten CSRF-Schutz; die Authentifizierung wird nicht gemockt. Separat prüft ein PostgreSQL-Testcontainer frische/wiederholte Migration, Modelldrift, Readiness, Identity-Anmeldung, fremden Mandantenzugriff und SQL-Auditmanipulation. Der Backend-Stand `0d4d688` hat diese Prüfungen in [CI-Lauf 33049169674](https://github.com/AdrahA92/revola-agent/actions/runs/33049169674) bestanden. Docker bleibt lokal nicht verfügbar. Neue UI-Vertragstests verwenden ausdrücklich gemockte API-Antworten und ersetzen keine vollständige Ende-zu-Ende-Abnahme mit echter Datenbank.

Siehe [ADR 0008](adr/0008-aspnet-identity.md) für Entscheidungen und Grenzen.

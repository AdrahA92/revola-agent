# ADR 0008: ASP.NET Core Identity und organisationsbezogene Rollen

- Status: Identity-Auswahl durch Product Owner bestätigt; Backend umgesetzt, Phase 2 noch in Arbeit
- Datum: 2026-08-27

## Entscheidung

ASP.NET Core Identity verwaltet Benutzer und Passwort-Hashes in PostgreSQL. Es gibt keinen externen Identity Provider. Die Browseranmeldung verwendet HttpOnly-/SameSite-Strict-Cookies und serverseitig validierte CSRF-Tokens, keine Browser-LocalStorage-Tokens. Nicht-Development-Cookies benötigen HTTPS. Fehlanmeldungen werden durch Identity-Lockout und IP-Rate-Limits begrenzt.

Organisationsrollen sind nicht globale Identity-Rollen: Eine Mitgliedschaft trägt genau eine der sechs Rollen. Jeder Serviceaufruf löst Benutzer und aktive Mitgliedschaft erneut auf. Eine Tenant-ID aus einer Route wird niemals als Berechtigung akzeptiert. Abfragen werden ausdrücklich nach Benutzer beziehungsweise TenantId eingeschränkt; globale EF-Filter werden nicht als Sicherheitsgrenze verwendet. Einladungen gelten erst nach Annahme durch den angemeldeten Zielbenutzer. Es gibt weder Benutzerverzeichnis noch automatische Einladungs-E-Mails.

Ein Owner kann nicht entfernt oder umgestuft werden. Weitere Owner und Eigentumsübertragung werden erst mit einem gesonderten bestätigten Ablauf eingeführt. Admins dürfen keine Owner/Admins bearbeiten und keine Admins ernennen. Manager, Editor, Approver und Viewer besitzen in der Mitgliederverwaltung keine Schreibrechte; ihre fachlichen Rechte folgen mit den jeweiligen Modulen.

Organisationsanlage und Einladungsanlage verwenden stabile Ressourcen-IDs für Wiederholungen. Mitgliedschaftsänderungen verwenden Versions-IDs und serialisierbare Transaktionen. Stale Updates liefern 409; Clients müssen neu laden, nicht blind wiederholen. Audit-Einträge werden atomar mit Änderungen geschrieben und sind im EF-Kontext sowie nach PostgreSQL-Migration gegen UPDATE/DELETE geschützt. Datenbankadministratoren bleiben eine separate Vertrauensgrenze; dies ist kein externes WORM-Archiv.

## Sicherheitsgrenzen und offene Arbeiten

- Registrierung ohne E-Mail-Bestätigung ist ausschließlich im Development-Modus möglich. Produktionsregistrierung bleibt geschlossen, solange Versand, Bestätigung und Passwortwiederherstellung nicht implementiert und geprüft sind.
- Logout widerruft alle Sitzungen über den Security Stamp; der nächste Request einer bestehenden Sitzung wird abgewiesen. Passwortänderung widerruft ebenfalls vorhandene Sitzungen.
- MFA-Oberfläche, Sitzungsübersicht und gezielter Einzelwiderruf sind noch offen. Benutzer mit bereits aktivierter MFA erhalten keine Anmeldung über den unvollständigen Passwort-Flow.
- Produktionsbetrieb benötigt HTTPS und einen persistenten, geschützten Data-Protection-Keyring. Ohne diesen sind Cookie-Schlüssel nicht deploymentübergreifend stabil.
- Rate Limits sind pro Prozess/IP, nicht clusterweit; Forwarded Headers werden nicht blind vertraut.
- Die React-Oberfläche für Phase 2 und die Komponentenbibliothek sind noch offen. Diese Änderung erweitert zunächst den Backend-Kern und behauptet keine vollständige Phase-2-Abnahme.

## Referenzen

- [ASP.NET Core Identity konfigurieren](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [CSRF-Schutz](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

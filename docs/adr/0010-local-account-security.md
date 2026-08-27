# ADR 0010: Lokale Kontosicherheit

Status: Implementiert, kein Produktivversand.

Bestätigung und Passwortwiederherstellung verwenden ASP.NET Core Identity und Data-Protection-Tokens mit einer Stunde Laufzeit. Die HTTP-Verträge transportieren Tokens im POST-Body, nicht in URLs. Bekannte und unbekannte Adressen erhalten bei erneuter Anforderung dieselbe generische Antwort. Registrierung bleibt Development-only und rollt bei nicht erreichbarem SMTP-Capture zurück. Ein Mailpit-Container nimmt Nachrichten lokal an; SMTP-Relay ist nicht konfiguriert. Tests ersetzen die Zustellung durch einen In-Memory-Sammler.

MFA verwendet den Identity-Authenticator-Provider und zehn einmalige Recovery-Codes. Einrichtung erfordert erneute Passwortprüfung; Aktivierung zusätzlich einen gültigen TOTP-Code. Verwendete TOTP-Codes werden gehasht und gegen kurzfristige Wiederverwendung geschützt; Identity-Concurrency verhindert paralleles Wiedereinlösen desselben Stands. MFA-Änderungen und Passwortänderungen beenden alle Sitzungen. Passwort-Recovery deaktiviert MFA ausdrücklich nicht.

Jede Anmeldung persistiert eine zeitlich begrenzte Sitzung. Das Cookie bindet deren ID und wird bei jeder Anfrage gegen den aktiven Datenbankeintrag geprüft. Listen und Widerrufe filtern ausschließlich auf den authentifizierten Benutzer. Die Migration beendet faktisch die Nutzbarkeit älterer Cookies ohne Sitzungs-ID.

Offen für Produktion: verschlüsselte persistente Schlüsselverwaltung, Verschlüsselung der Authenticator-Secrets im Ruhezustand, bestätigter Zustelldienst, MFA-Pflicht für privilegierte Rollen, Aufbewahrungsfristen/Sitzungsbereinigung und vollständige Browser-/Accessibility-Abnahme. Es wird keine Produktionsreife behauptet.

Referenzen: [Microsoft MFA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/mfa?view=aspnetcore-10.0), [Identity-Bestätigung und Recovery](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0), [Mailpit Docker](https://mailpit.axllent.org/docs/install/docker/).

# ADR 0012: Demo-Agent, Content-Freigaben und manueller Browser-Fallback

Status: Entwicklungsmodus implementiert, 28.08.2026.

## Entscheidung

Die Runtime ist zunächst deterministisch und ohne externe Aufrufe. Der OpenAI-Responses-Vertrag ist vorbereitet, aber kein HTTP-Client registriert. Unternehmensdaten und Ziele erlauben keine zusätzlichen Tools. Zulässig sind ausschließlich Profil lesen und Entwurf erzeugen; Kontingente und Laufzeit begrenzen die Ausführung. Vor Speicherung eines Ergebnisses werden Rechte erneut geprüft. Abgelaufene Running-Einträge erscheinen als TimedOut; ein persistenter Wiederanlauf-/Bereinigungsworker ist noch nicht implementiert.

Content-Versionen und Entscheidungen sind in EF und PostgreSQL append-only. Freigaben binden den SHA-256-Hash der vollständigen Nutzdaten einschließlich Ziel, Termin und Zeitzone. Autoren können ihre eigene Version nicht genehmigen. Änderungen setzen den Status auf Draft zurück. Terminplanung prüft aktuelle Rechte des Freigebenden und Ablauf erneut. Sie löst keine Veröffentlichung aus.

## Schnittstellen

Alle Routen liegen unter `/api/tenants/{tenantId}`, benötigen Anmeldung und aktive Mitgliedschaft. Schreibzugriffe prüfen Rollen und CSRF.

- `GET agent-runs`, `PUT agent-runs/{id}`: Verlauf und idempotente Demo-Ausführung.
- `GET content`, `PUT content/{id}`: aktuelle Entwürfe und versionierte Speicherung.
- `POST content/{id}/decision`: submit, approve, reject, schedule oder cancel.
- `GET content/{id}/history`: unveränderliche Versionshistorie.
- `GET connections`: verfügbare manuelle Arbeitsweisen, keine verbundenen Konten.

## Browser-Anmeldung

Der gewünschte Browser-Fallback besteht aus separat geöffneten Plattformseiten und explizitem Kopieren von Text. Es gibt keine Browserautomatisierung, Passwortübernahme, Sitzungsextraktion oder Verbindungserkennung. `Connected`, `CanReadAccount` und `CanPublish` bleiben false. Manuelles Kopieren ist keine Veröffentlichung und umgeht nicht die Freigabe für spätere automatische Aktionen.

## Grenzen und Inbetriebnahme

Die Migration `20260828180156_AgentContent` muss vor Nutzung angewendet werden. Neue Endpunkte und Oberflächen werden durch Mandanten-, Rollen-, Idempotenz-, Kontingent- und Freigabetests abgedeckt. PostgreSQL-Migration und SQL-Schreibschutz werden zusätzlich im Docker-CI-Test geprüft. Visuelle Abnahme bleibt zurückgestellt.

Vor Livebetrieb: erste Plattform und minimale Scopes bestätigen, OAuth-App konfigurieren, verschlüsselte Tokenablage und Widerruf implementieren, Provider-Modell und Budget festlegen, Medien-/Speicherentscheidung treffen und einen idempotenten Publishing-Worker mit erneuter Freigabeprüfung implementieren. Produktive E-Mail-Zustellung, Betriebshärtung, Analytics, CRM und Abrechnung bleiben eigene offene Arbeitspakete.

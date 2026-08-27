# Sicherheits- und Datenschutzkonzept

## Implementierungsstand

Phase 2 enthält bisher den Identity-/Tenancy-Backend-Kern: Cookie-Anmeldung, CSRF, Lockout, Rate Limits, Rollenprüfung pro Organisation, Versionskonflikte und append-only Auditierung. Registrierungen sind nur im Development-Modus möglich. MFA, E-Mail-Bestätigung, Recovery, Sitzungsübersicht und produktive Schlüsselverwaltung sind weiterhin Ziele, keine fertiggestellten Funktionen. Die folgenden Abschnitte beschreiben teilweise den angestrebten Produktzustand. Details und Testgrenzen: [Identity-/Tenancy-API](identity-tenancy-api.md).

## Schutzgüter

- Benutzerkonten und Sitzungen
- Mandantendaten und Unternehmenswissen
- OAuth-Tokens und API-Secrets
- unveröffentlichte Inhalte und Medien
- Kontakte, Leads und Kommunikationsentwürfe
- Freigaben und externe Aktionen
- Agentenprompts, Werkzeugaufrufe und Verbrauchsdaten

## Vertrauensgrenzen

Nicht vertrauenswürdig sind Browserdaten, Plattforminhalte, Webseiten, Nachrichten, Uploads, Modellantworten und Webhooks vor erfolgreicher Prüfung. Nur serverseitige Richtlinien dürfen Werkzeuge freigeben oder Risiken bewerten.

## Zentrale Bedrohungen und Maßnahmen

| Bedrohung | Maßnahme |
| --- | --- |
| Zugriff zwischen Mandanten | serverseitiger TenantContext, TenantId, Autorisierung, Isolationstests |
| gestohlene Plattformtokens | Verschlüsselung, minimale Scopes, Rotation, kein Frontendzugriff |
| Prompt Injection | externe Inhalte als Daten markieren, Werkzeug-Whitelist, Ausgabevalidierung |
| unfreigegebene Veröffentlichung | unveränderliche Version, Policy Gate, Approval Token |
| doppelte externe Aktion | Idempotency Key und gespeicherte externe Referenz |
| manipulierte Webhooks | Signatur, Zeitfenster, Replay-Schutz |
| Spam oder Massenansprache | Tageslimits, Deduplizierung, Sperrliste, Versandfreigabe |
| sensible Daten in Logs | zentrale Redaction und strukturierte Allowlist-Logs |
| Kostenmissbrauch | Budgets, Rate Limits, Laufzeit- und Schrittgrenzen |
| schädliche Uploads | Größen-/Typprüfung, isolierter Speicher, Malwareprüfung vor Nutzung |
| Supply-Chain-Angriff | Lockfiles, Updates, Dependency- und Secret-Scanning |

## Authentifizierung und Sitzungen

- sichere, kurzlebige Sitzungen mit Rotation
- MFA mindestens für Owner und Admin als Produktziel
- sichere Cookies, CSRF-Schutz und restriktives CORS
- Rate Limits und progressive Verzögerung bei fehlgeschlagenen Anmeldungen
- Sitzungsübersicht und Widerruf

## Autorisierung

Autorisierung prüft Organisation, aktive Mitgliedschaft, Rolle, Ressourcenzugehörigkeit und konkrete Aktion. Sensible Operationen dürfen nicht allein durch Besitz einer Ressourcen-ID möglich sein.

## Freigabesicherheit

Eine Freigabe speichert Hash und Version von Inhalt, Medien, Zielkonto, Empfänger, Aktion und Zeitpunkt. Sie verfällt bei Änderung, Ablauf, Rollenverlust oder widerrufener Verbindung. Das Sprachmodell kann keine Freigabe erzeugen.

## Secret- und Tokenverwaltung

- keine Secrets in Quellcode, Logs, Tests oder Prompts
- Verschlüsselung mit versionierten Schlüsseln
- getrennte Konfiguration pro Umgebung
- Rotation ohne Neuverschlüsselungsausfall
- Tokens werden nur im Plattformadapter entschlüsselt

## Datenschutz

- Datenminimierung und zweckgebundene Verarbeitung
- dokumentierte Quellen für Leads und Wissenseinträge
- Aufbewahrungsfristen je Datenklasse
- Export und Löschung eines Mandanten
- Sperrlisten bleiben soweit rechtlich erforderlich als minimale Hash-/Kontaktsperre erhalten
- keine unnötigen personenbezogenen Daten im Modellkontext
- Betreiber- und Unterauftragsverarbeiter müssen vor Produktivbetrieb dokumentiert werden

## Aufbewahrungsvorschlag

| Datenart | Vorgabe vor finaler Rechtsprüfung |
| --- | --- |
| aktive Unternehmensdaten | bis Vertragsende oder Löschung |
| Agentenlauf-Inhalte | 90 Tage, Metadaten länger aggregiert |
| technische Logs | 30 Tage |
| Auditereignisse | 12 Monate oder vertragliche Vorgabe |
| abgelehnte Entwürfe | 90 Tage |
| OAuth-Tokens | bis Widerruf oder Trennung |
| Leads ohne Aktivität | 6 Monate |

Diese Fristen sind Produktannahmen und müssen vor Produktivbetrieb rechtlich geprüft werden.

## Sicherheitsprüfungen vor Produktivbetrieb

- Threat-Model-Review
- Mandanten-Isolationstest
- Berechtigungs- und IDOR-Test
- Prompt-Injection- und Tool-Missbrauchstests
- Secret- und Dependency-Scan
- Wiederherstellungstest
- Datenschutz- und Löschtest
- Prüfung der Plattformbedingungen
- externer Penetrationstest vor breiter Einführung

## Sicherheitsvorfälle

Jeder Vorfall erhält Korrelations-ID, Zeitlinie, betroffene Mandanten, Eindämmung, Behebung und Nachprüfung. Tokenkompromittierung löst Widerruf und Rotation aus. Logs dürfen während der Analyse nicht unkontrolliert exportiert werden.

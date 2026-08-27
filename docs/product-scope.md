# Produktumfang

## Produktziel

Revola Agent ist eine mandantenfähige SaaS, die kleine Unternehmen, Agenturen und Freelancer bei der professionellen Betreuung ihrer verbundenen Unternehmensprofile unterstützt. Ein Unternehmensagent analysiert Konten, erstellt nachvollziehbare Empfehlungen, bereitet Inhalte vor und führt freigegebene Aktionen über offiziell unterstützte Plattform-Schnittstellen aus.

Der erste messbare Nutzen ist ein durchgängiger Ablauf vom Unternehmens-Onboarding über ein erklärbares Konto-Audit bis zum freigegebenen und geplanten Social-Media-Beitrag.

## Zielgruppen

1. Kleine und mittlere Unternehmen ohne eigenes Social-Media-Team
2. Freelancer und Softwaredienstleister mit Bedarf an kontinuierlicher Sichtbarkeit
3. Marketingagenturen, die mehrere Mandanten betreuen
4. Unternehmen mit regionaler Kunden- oder Recruiter-Akquise

## Kernprobleme

- Unternehmensprofile sind unvollständig oder veraltet.
- Inhalte werden unregelmäßig und ohne messbare Strategie veröffentlicht.
- Verbesserungsvorschläge sind häufig allgemein und nicht nachvollziehbar.
- Freigaben, Planung, Kennzahlen und Leads liegen in getrennten Werkzeugen.
- Vollautomatische Lösungen bergen Risiken durch falsche Aussagen, Spam oder unzulässige Plattformnutzung.

## MVP-Umfang

### Enthalten

- Registrierung, Organisationen, Mitglieder und Rollen
- Mandantensicheres Unternehmens- und Markenprofil
- Zielgruppen, Regionen, Leistungen, Kontaktwege und Wissenseinträge
- Demo-Plattformverbindung mit realistischen Beispieldaten
- deterministisches Konto-Audit mit Teilwerten und Score von 0 bis 100
- priorisierte, begründete Verbesserungsvorschläge
- Agenten-gestützte Erstellung von Content-Briefings und Textentwürfen
- Bildbriefings und Medienreferenzen
- versionierte Inhalte und Freigaben
- Redaktionskalender und simulierte Veröffentlichung
- Agentenaktivität, Verbrauchsdaten und Auditprotokoll
- responsive Weboberfläche

### Nach dem MVP

- erste echte Social-Media-Verbindung nach bestätigter API-Machbarkeit
- regelmäßige Audits und Analytics
- Lead- und Recruiter-Recherche aus zulässigen Quellen
- CRM, Outreach-Entwürfe und Nachverfolgung
- Abrechnung, Kontingente und Agenturmodus

## Nicht-Ziele

- Speicherung von Social-Media-Passwörtern
- Browserautomatisierung als Produktionsintegration
- Umgehung von API-Limits oder Plattformrichtlinien
- automatische Freundschaftsanfragen oder Massen-Einladungen
- ungeprüfte Massenkommunikation
- autonome Werbeausgaben, Käufe oder verbindliche Angebote
- Ersetzung eines vollständigen Enterprise-CRM im MVP
- Microservice-Architektur ohne belegten Bedarf

## Rollen

| Rolle | Hauptrechte |
| --- | --- |
| Owner | Organisation, Richtlinien, Verbindungen und Abrechnung |
| Admin | Mitglieder, Profile, Inhalte und Freigaben |
| Manager | Audits, Inhalte, Agentenläufe, Leads und Analysen |
| Editor | Entwürfe erstellen und bearbeiten |
| Approver | versionierte Entwürfe freigeben oder ablehnen |
| Viewer | Inhalte und Ergebnisse lesen |

## Kernabläufe

### Onboarding und Audit

1. Benutzer erstellt eine Organisation.
2. Er ergänzt Unternehmens-, Marken- und Zielgruppendaten.
3. Er verbindet zunächst einen Demo-Account.
4. Das System erfasst verfügbare Kontodaten.
5. Die Audit-Engine berechnet Teilwerte und Gesamtscore.
6. Der Agent erläutert die Ergebnisse und erstellt priorisierte Empfehlungen.
7. Der Benutzer übernimmt, verwirft oder verschiebt Empfehlungen.

### Beitragserstellung

1. Der Benutzer erstellt ein Briefing oder übernimmt eine Empfehlung.
2. Der Agent erzeugt einen Entwurf und ein Bildbriefing.
3. Der Editor überarbeitet die Version.
4. Ein Approver bestätigt die unveränderliche Version.
5. Der Beitrag wird geplant und über den Demo-Adapter veröffentlicht.
6. Status und externe Referenz werden gespeichert.

### Kontinuierliche Begleitung

1. Ein Zeitplan startet ein neues Audit.
2. Das System vergleicht Werte und frühere Empfehlungen.
3. Der Agent erzeugt nur neue oder aktualisierte Hinweise.
4. Der Kunde erhält eine priorisierte Wochenzusammenfassung.
5. Nach umgesetzten Maßnahmen wird deren Wirkung kontrolliert.

## Erfolgskennzahlen

- Anteil vollständig abgeschlossener Onboardings
- Zeit vom Onboarding bis zum ersten Audit
- Anteil nachvollziehbarer Auditkriterien mit vorhandener Datengrundlage
- Annahmequote von Empfehlungen
- Zeit vom Briefing bis zur Freigabe
- erfolgreiche, nicht doppelte Veröffentlichungen
- Anzahl erkannter und verhinderter unzulässiger Aktionen
- Entwicklung von Profilscore, Klicks und qualifizierten Anfragen

## Produktentscheidungen, die vor Phase 7 bestätigt werden müssen

- erste produktive Social-Media-Plattform
- konkret benötigte OAuth-Berechtigungen
- unterstützte Kontotypen und Regionen
- zulässiger Umfang von Analytics und Profildaten
- Betreiberregion, Auftragsverarbeitung und Datenaufbewahrung

# ADR 0004: Ausschließlich offizielle Plattform-APIs

- Status: Angenommen
- Datum: 2026-08-26

## Kontext

Browserautomatisierung könnte kurzfristig zusätzliche Funktionen ermöglichen, ist aber instabil, sicherheitskritisch und häufig nicht mit Plattformregeln vereinbar.

## Entscheidung

Produktive Social-Media-Integrationen verwenden ausschließlich offiziell unterstützte APIs und OAuth. Nicht verfügbare Funktionen werden transparent als nicht unterstützt angezeigt.

## Konsequenzen

- stabilere und regelkonforme Integrationen
- Funktionsumfang hängt von Plattform, Accounttyp und Review ab
- Capability-Erkennung wird Teil des Produktmodells

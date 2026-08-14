# Calculator API Tests

API-Tests für die Calculator-API auf Basis von **NUnit** und **Microsoft Playwright** (API-Request-Kontext, keine Browser-Tests), mit **Allure**-Reporting.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Die Calculator-API wird **nicht** aus diesem Repository gestartet, sondern muss extern laufen (Standard: `http://localhost:5116`).

## Konfiguration

| Umgebungsvariable | Beschreibung | Standard |
|---|---|---|
| `API_BASE_URL` | Basis-URL der Calculator-API | `http://localhost:5116` |

Beispiel (PowerShell):

```powershell
$env:API_BASE_URL = "http://localhost:5116"
```

## Tests ausführen

```powershell
dotnet test
```

Ist die API nicht erreichbar, werden die Tests mit einer klaren Meldung als *Inconclusive* markiert.

## Teststruktur

| Fixture | Inhalt |
|---|---|
| `HappyPathTests` | Grundrechenarten (200 OK) |
| `FloatingPointTests` | Gleitkomma-Randfälle (Toleranz, Überlauf) |
| `DivisionByZeroTests` | Division durch null (400) |
| `ValidationTests` | Eingabevalidierung über alle 4 Endpunkte (400/415) |
| `ContractTests` | API-Vertrag / Robustheit (Felder, Content-Type, 405, 404) |

## Allure-Report

Die Ergebnisse werden gemäß `allureConfig.json` in `allure-results` abgelegt und können z. B. mit `allure serve` angezeigt werden.

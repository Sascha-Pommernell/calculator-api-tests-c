# Calculator API Tests

API-Tests für die **Calculator API** (separates Projekt `calculator-api`) auf Basis von **NUnit** und **Microsoft Playwright** (API-Request-Kontext, keine Browser-Tests), mit **Allure**-Reporting.

Das zugehörige Testkonzept liegt im API-Projekt unter `docs/Testkonzept.md`.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Die Calculator-API wird **nicht** aus diesem Repository gestartet, sondern muss extern laufen (Standard: `http://localhost:5116`), z. B. via:

  ```powershell
  dotnet run --project ..\calculator-api\Calculator.Api\Calculator.Api
  ```

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

Mit TRX-Testbericht:

```powershell
dotnet test --logger "trx;LogFileName=testbericht.trx" --results-directory TestResults
```

Ist die API nicht erreichbar, werden die Tests mit einer klaren Meldung als *Inconclusive* markiert.

## Tests in Docker ausführen

Das `Dockerfile` baut das Testprojekt, führt die Tests aus und legt TRX- sowie Allure-Ergebnisse in `/results` ab (per Volume mounten):

```powershell
docker build -t calculator-api-tests .
docker run --rm `
  -e API_BASE_URL=http://host.docker.internal:5116 `
  -v ${PWD}\TestResults:/results `
  calculator-api-tests
```

> Hinweis: `host.docker.internal` verwenden, wenn die API auf dem Host läuft; alternativ die Container beider Projekte in ein gemeinsames Docker-Netzwerk hängen.

## Teststruktur

| Fixture | Inhalt |
|---|---|
| `HappyPathTests` | Grundrechenarten (200 OK) |
| `FloatingPointTests` | Gleitkomma-Randfälle (Toleranz, Überlauf) |
| `DivisionByZeroTests` | Division durch null (400) |
| `ValidationTests` | Eingabevalidierung über alle 4 Endpunkte (400/415) |
| `ContractTests` | API-Vertrag / Robustheit (Felder, Content-Type, 405, 404) |

## Allure-Report

Die Ergebnisse werden gemäß [CalculatorApiTests/allureConfig.json](CalculatorApiTests/allureConfig.json) in `allure-results` (im Build-Ausgabeverzeichnis) abgelegt und können z. B. mit `allure serve` angezeigt werden:

```powershell
allure serve CalculatorApiTests\bin\Debug\net10.0\allure-results
```

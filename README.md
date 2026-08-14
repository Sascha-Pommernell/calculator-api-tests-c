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
| `CI` | Bei `true`: nicht erreichbare API führt zu hartem Fehlschlag statt *Inconclusive* | (leer) |

Beispiel (PowerShell):

```powershell
$env:API_BASE_URL = "http://localhost:5116"
```

Vor dem Testlauf wird die Erreichbarkeit der API über `GET /health` geprüft.

## Tests ausführen

```powershell
dotnet test
```

Die Fixtures laufen parallel (NUnit `Parallelizable`, 4 Worker). Nur die hochprioren Testgruppen (risikobasierte Priorisierung, siehe Testkonzept Kap. 3.4):

```powershell
dotnet test --filter "TestCategory=Prio-Hoch"
```

Mit TRX-Testbericht:

```powershell
dotnet test --logger "trx;LogFileName=testbericht.trx" --results-directory TestResults
```

Ist die API nicht erreichbar, werden die Tests lokal mit einer klaren Meldung als *Inconclusive* markiert; in CI (`CI=true`) schlagen sie stattdessen fehl, damit die Pipeline nicht „grün ohne Tests“ wird.

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

| Fixture | Priorität | Inhalt |
|---|---|---|
| `HappyPathTests` | Hoch | Grundrechenarten (200 OK) |
| `FloatingPointTests` | Mittel | Gleitkomma-Randfälle (Toleranz, Überlauf) |
| `DivisionByZeroTests` | Hoch | Division durch null (400) |
| `ValidationTests` | Hoch | Eingabevalidierung über alle 4 Endpunkte (400/415, inkl. Obergrenze 1000 Zahlen) |
| `ContractTests` | Mittel | API-Vertrag / Robustheit (Felder, Content-Type, 405, 404) |

Die Prioritäten sind als NUnit-Kategorien (`Prio-Hoch`/`Prio-Mittel`) und Allure-Severity hinterlegt.

## CI (GitHub Actions)

Der Workflow [.github/workflows/tests-ci.yml](.github/workflows/tests-ci.yml) baut bei Push/PR auf `main` beide Docker-Images (API-Repo wird ausgecheckt, Ref per `workflow_dispatch` wählbar), startet die API mit Produktionskonfiguration, wartet auf `GET /health`, führt die Tests im gemeinsamen Docker-Netzwerk aus und veröffentlicht TRX-Ergebnisse als Check sowie TRX + Allure-Rohdaten als Artefakt (14 Tage).

## Allure-Report

Die Ergebnisse werden gemäß [CalculatorApiTests/allureConfig.json](CalculatorApiTests/allureConfig.json) in `allure-results` (im Build-Ausgabeverzeichnis) abgelegt und können z. B. mit `allure serve` angezeigt werden:

```powershell
allure serve CalculatorApiTests\bin\Debug\net10.0\allure-results
```

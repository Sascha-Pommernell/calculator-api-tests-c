// API-Tests sind zustandslos; Fixtures laufen parallel (jede Fixture nutzt ihre eigene Playwright-Instanz).
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]

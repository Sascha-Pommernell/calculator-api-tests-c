FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /tests

COPY CalculatorApiTests/CalculatorApiTests.csproj CalculatorApiTests/
RUN dotnet restore CalculatorApiTests/CalculatorApiTests.csproj

COPY CalculatorApiTests/ CalculatorApiTests/
RUN dotnet build CalculatorApiTests/CalculatorApiTests.csproj -c Release --no-restore

# Führt die Tests aus und kopiert TRX- und Allure-Ergebnisse nach /results (per Volume mounten).
ENTRYPOINT ["/bin/bash", "-c", "\
    dotnet test CalculatorApiTests/CalculatorApiTests.csproj -c Release --no-build \
      --logger 'trx;LogFileName=testbericht.trx' --results-directory /results/trx; \
    ec=$?; \
    src=$(find /tests -type d -name allure-results | head -n 1); \
    if [ -n \"$src\" ]; then mkdir -p /results && cp -r \"$src\" /results/; fi; \
    exit $ec"]

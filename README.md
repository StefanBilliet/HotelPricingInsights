# Hotel Pricing Insights

This repository implements a technical exercise that centres on comparing hotel prices for a requested month with historical ("pre-corona") prices. The API fetches current and historical pricing extracts, normalises everything to a target currency, and exposes the results through a single HTTP endpoint.

## Solution Overview

- **Business context**: Given a set of hotels, a target month, target currency, and the number of years to look back, the service returns the most recent lowest prices for each hotel and the price difference versus the same arrival date `years_ago` years earlier.

## Architecture

The runtime is an ASP.NET Core Web API targeting .NET 9.0. The main pipeline consists of:

1. **Endpoint layer** (`PricingComparisonEndpoint`) – validates query parameters with FluentValidation, parses the arrival month, and delegates to the comparison service.
2. **Application service** (`HotelPricingComparisonService`) – retrieves current/historical extracts in parallel, filters for cancellable rates when requested, converts rates to the target currency, and calculates the price differences.
3. **Data services** – 
   - `PricingExtractsForHotelsInSpecificPeriodDataService` reads pricing extracts from Bigtable (the harness fakes the dependencies).
   - `MonthAnchoredCurrencyExchangeRatesDataService` fetches exchange rates via a supplied `IDbConnection` factory.
   - `CachingCurrencyExchangeRatesDataService` decorates the exchange-rate service with a Polly-backed memory cache.
4. **Cross-cutting concerns** – OpenTelemetry captures logs, traces, and metrics, exporting to a placeholder OTLP endpoint; the sequence diagram in `docs/sequence-diagram.mmd` visualises the full request flow.

All services are wired through `ServiceCollectionExtensions.AddPricingComparisonServices`, which is exercised in unit tests to validate dependency composition.

## Project Layout

```
├── HotelPricingInsights/          # ASP.NET Core application
│   ├── Controllers/
│   │   └── PricingComparisonEndpoint.cs
│   ├── Controllers/HotelPriceComparison/
│   │   ├── HotelPriceComparisonService.cs
│   │   ├── PricingExtractsForHotelsInSpecificPeriod/
│   │   └── CurrencyExchangeRates/
│   └── ServiceCollectionExtensions.cs
├── Tests/                         # xUnit test project
│   ├── Web/…                      # Unit tests for endpoints & services
│   └── CurrencyExchangeRates/…    # Tests for caching behaviour
├── docs/
│   ├── EXTERNAL_openapi_(4).yml   # OpenAPI contract
│   ├── sequence-diagram.mmd       # Mermaid sequence diagram
│   └── EXTERNAL_2025_case_…pdf    # Exercise briefing
└── .github/workflows/dotnet.yml   # CI pipeline
```

## Getting Started

```bash
# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Run the test suite
dotnet test --configuration Release
```

By default the application exports OpenTelemetry data to `http://localhost:4317`; update `Program.cs` if you want to plug in a real collector.
## Slide 1 · Context & Goal
- Build an API that compares current hotel prices with the same month _x_ years ago.
- Consumers pass hotel IDs, arrival month, target currency, years back, and an optional `cancellable` filter.
- Response: one record per hotel/arrival day with the latest price plus the historical difference.

## Slide 2 · What Stood Out in the Brief
- Bigtable rows are keyed as `hotelId#extractDate#arrivalDate`, while the use case needs lookups by *hotel + arrival date*.
- Pricing data is stored in the original sale currency; no canonical “system currency”.
- The brief doesn’t spell out how often extracts arrive or how accurate historic FX rates need to be.

## Slide 3 · Working Assumptions
- Recent extracts exist within the month leading up to the arrival date → I query a one‑month look-back window.
- Historical prices follow the same cadence, so the same window applies when looking `years_ago` in the past.
- Exchange rates can be normalised through USD and cached per month.

## Slide 4 · Solution Overview
- **Endpoint layer** (`PricingComparisonEndpoint`)
  - FluentValidation on query parameters, including the cancellable flag.
  - Parses the month, calls the comparison service, returns JSON.
- **Comparison service** (`HotelPricingComparisonService`)
  - Fetches current and historical extracts in parallel using the look-back window.
  - Keeps the latest extract per hotel/day, filters non-cancellable rates when requested.
  - Normalises to USD via the currency converter, then computes differences.
- **Supporting services**
  - `PricingExtractsForHotelsInSpecificPeriodDataService` for Bigtable access.
  - `MonthAnchoredCurrencyExchangeRatesDataService` wrapped by a Polly‑cached decorator.

## Slide 5 · Request Flow
- [sequence-diagram.mmd](sequence-diagram.mmd)

## Slide 6 · Handling the Data
- **Look-back window**: Hardcoded to one month.
- **Exchange rates**: fetched via the DB-backed service and cached for 10 minutes to avoid duplicate queries.
- **Cancellable filter**: applied before the currency conversion step to avoid leaking non-cancellable rates.
- **Missing data**: if no historical price exists within the window, the difference remains `null`.

## Slide 7 · Observability & Quality Gates
- OpenTelemetry instrumentation (ASP.NET Core, HttpClient, runtime metrics) with an OTLP exporter pointing to a placeholder endpoint.
- Tests cover:
  - Endpoint validation and error paths.
  - Comparison service scenarios (no data, current only, current + historical, cancellable filter).
  - Data-fetching.
  - Currency caching decorator behaviour.
  - DI composition, so controllers resolve the same way Program.cs wires them.
- GitHub Actions workflow restores, builds, and runs tests in Release mode.
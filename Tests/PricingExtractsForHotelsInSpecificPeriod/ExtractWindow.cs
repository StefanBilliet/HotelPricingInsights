namespace Tests.PricingExtractsForHotelsInSpecificPeriod;

public sealed record ExtractWindow(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtcExclusive
);
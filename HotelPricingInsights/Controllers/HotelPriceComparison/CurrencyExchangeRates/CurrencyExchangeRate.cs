namespace HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyExchangeRates;

public readonly record struct CurrencyExchangeRate(
    string Currency,      
    decimal UsdConversionRate,
    DateOnly ExtractDate
);
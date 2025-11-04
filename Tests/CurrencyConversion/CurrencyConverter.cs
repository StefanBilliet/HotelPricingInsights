using Tests.CurrencyExchangeRates;

namespace Tests.CurrencyConversion;

public interface ICurrencyConverter
{
    Task<PriceInfo?> ConvertPrice(
        PriceInfo price,
        string targetCurrency,
        DateOnly monthAnchor,
        CancellationToken cancellationToken);
}

public class CurrencyConverter : ICurrencyConverter
{
    private readonly ICurrencyExchangeRatesDataService _exchangeRatesService;

    public CurrencyConverter(ICurrencyExchangeRatesDataService exchangeRatesService)
    {
        _exchangeRatesService = exchangeRatesService;
    }

    public async Task<PriceInfo?> ConvertPrice(
        PriceInfo price,
        string targetCurrency,
        DateOnly monthAnchor,
        CancellationToken cancellationToken)
    {
        if (price.Currency == targetCurrency)
        {
            return price;
        }

        var rateToUsd = await _exchangeRatesService.GetForCurrency(price.Currency, monthAnchor, cancellationToken);
        if (rateToUsd == null)
        {
            return null;
        }

        var priceInUsd = Math.Round(price.PriceValue * rateToUsd.Value.UsdConversionRate, 2);

        return price with 
        { 
            PriceValue = priceInUsd, 
            Currency = "USD" 
        };
    }
}
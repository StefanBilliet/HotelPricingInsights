using System.Globalization;
using Polly;
using Polly.Caching;

namespace HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyExchangeRates;

public class CachingCurrencyExchangeRatesDataService : ICurrencyExchangeRatesDataService
{
    private readonly ICurrencyExchangeRatesDataService _decorated;
    private readonly AsyncCachePolicy<CurrencyExchangeRate?> _cachePolicy;

    public CachingCurrencyExchangeRatesDataService(
        ICurrencyExchangeRatesDataService decorated,
        AsyncCachePolicy<CurrencyExchangeRate?> cachePolicy)
    {
        _decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
        _cachePolicy = cachePolicy ?? throw new ArgumentNullException(nameof(cachePolicy));
    }

    public Task<CurrencyExchangeRate?> GetForCurrency(string currency, DateOnly monthAnchor, CancellationToken cancellationToken)
    {
        var context = new Context(BuildCacheKey(currency, monthAnchor));

        return _cachePolicy.ExecuteAsync(
            (_, innerCancellationToken) => _decorated.GetForCurrency(currency, monthAnchor, innerCancellationToken),
            context,
            cancellationToken);
    }

    private static string BuildCacheKey(string currency, DateOnly monthAnchor) =>
        $"{currency}:{monthAnchor.ToString("O", CultureInfo.InvariantCulture)}";
}

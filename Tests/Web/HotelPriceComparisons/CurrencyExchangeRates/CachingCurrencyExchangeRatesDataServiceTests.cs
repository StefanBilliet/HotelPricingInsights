using FakeItEasy;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyExchangeRates;
using Polly;
using Tests.Infrastructure;

namespace Tests.Web.HotelPriceComparisons.CurrencyExchangeRates;

public class CachingCurrencyExchangeRatesDataServiceTests
{
    private readonly ICurrencyExchangeRatesDataService _decorated;
    private readonly CachingCurrencyExchangeRatesDataService _sut;
    private static readonly DateOnly MonthAnchor = new(2024, 5, 1);
    private static readonly CurrencyExchangeRate SampleRate = new("EUR", 1.1m, MonthAnchor);

    public CachingCurrencyExchangeRatesDataServiceTests()
    {
        var cachePolicy = Policy.CacheAsync<CurrencyExchangeRate?>(new DummyInMemoryCache(), TimeSpan.FromMinutes(5));
        _decorated = A.Fake<ICurrencyExchangeRatesDataService>();
        _sut = new CachingCurrencyExchangeRatesDataService(_decorated, cachePolicy);
    }

    [Fact]
    public async Task GIVEN_cached_result_WHEN_called_with_same_currency_and_anchor_THEN_returns_cached_value()
    {
        A.CallTo(() => _decorated.GetForCurrency("EUR", MonthAnchor, CancellationToken.None))
            .Returns(SampleRate);

        var firstCall = await _sut.GetForCurrency("EUR", MonthAnchor, CancellationToken.None);
        var secondCall = await _sut.GetForCurrency("EUR", MonthAnchor, CancellationToken.None);

        Assert.Equal(SampleRate, firstCall);
        Assert.Equal(SampleRate, secondCall);
        A.CallTo(() => _decorated.GetForCurrency("EUR", MonthAnchor, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GIVEN_different_cache_key_WHEN_called_THEN_invokes_decorated_each_time()
    {
        var usdRate = new CurrencyExchangeRate("USD", 1m, MonthAnchor);
        A.CallTo(() => _decorated.GetForCurrency("USD", MonthAnchor, CancellationToken.None))
            .Returns(usdRate);
        A.CallTo(() => _decorated.GetForCurrency("EUR", MonthAnchor.AddMonths(1), CancellationToken.None))
            .Returns(SampleRate with { ExtractDate = MonthAnchor.AddMonths(1) });

        _ = await _sut.GetForCurrency("USD", MonthAnchor, CancellationToken.None);
        _ = await _sut.GetForCurrency("EUR", MonthAnchor.AddMonths(1), CancellationToken.None);

        A.CallTo(() => _decorated.GetForCurrency("USD", MonthAnchor, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _decorated.GetForCurrency("EUR", MonthAnchor.AddMonths(1), CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }
}
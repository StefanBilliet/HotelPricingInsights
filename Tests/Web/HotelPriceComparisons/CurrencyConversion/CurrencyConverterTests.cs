using AutoFixture;
using AutoFixture.Xunit3;
using FakeItEasy;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyConversion;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyExchangeRates;
using Tests.Infrastructure;

namespace Tests.Web.HotelPriceComparisons.CurrencyConversion;

public class CurrencyConverterTests
{
    private readonly CurrencyConverter _sut;
    private readonly ICurrencyExchangeRatesDataService _currencyExchangeRatesDataService;
    private readonly Fixture _fixture;

    public CurrencyConverterTests()
    {
        _currencyExchangeRatesDataService = A.Fake<ICurrencyExchangeRatesDataService>();
        _sut = new CurrencyConverter(_currencyExchangeRatesDataService);
        _fixture = AutoFixtureFactory.Instance;
    }

    [Theory, AutoData]
    public async Task WHEN_price_is_already_in_target_currency_THEN_returns_price(PriceInfo priceInfo)
    {
        var result = await _sut.ConvertPrice(priceInfo, priceInfo.Currency, DateOnly.FromDateTime(DateTime.Today), TestContext.Current.CancellationToken);

        Assert.Equal(priceInfo, result);
    }

    [Theory, AutoData]
    public async Task GIVEN_no_matching_currency_conversion_record_WHEN_ConvertPrice_THEN_returns_null(PriceInfo priceInfo)
    {
        var result = await _sut.ConvertPrice(priceInfo, "BTC", DateOnly.FromDateTime(DateTime.Today), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GIVEN_price_is_not_in_target_currency_WHEN_ConvertPrice_THEN_returns_price_in_target_currency()
    {
        var monthAnchor = DateOnly.FromDateTime(DateTime.Today);
        A.CallTo(() => _currencyExchangeRatesDataService.GetForCurrency( "EUR", monthAnchor, A<CancellationToken>._)).Returns(new CurrencyExchangeRate("EUR", 1.1295m, monthAnchor));
        var priceInfo = _fixture
            .Build<PriceInfo>()
            .With(price => price.Currency, "EUR")
            .With(price => price.PriceValue, 112.95m)
            .Create();
        
        var result = await _sut.ConvertPrice(priceInfo, "USD", monthAnchor, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(127.58m, result.PriceValue);
    }
}
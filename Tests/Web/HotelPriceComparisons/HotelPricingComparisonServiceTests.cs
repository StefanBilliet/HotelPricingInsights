using AutoFixture;
using AutoFixture.Xunit3;
using FakeItEasy;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyConversion;
using HotelPricingInsights.Controllers.HotelPriceComparison.PricingExtractsForHotelsInSpecificPeriod;
using Tests.Infrastructure;

namespace Tests.Web.HotelPriceComparisons;

public class HotelPricingComparisonServiceTests
{
    private readonly HotelPricingComparisonService _sut;
    private readonly IPricingExtractsForHotelsInSpecificPeriodDataService _pricingExtractsForHotelsInSpecificPeriodDataService;
    private readonly Fixture _fixture;

    public HotelPricingComparisonServiceTests()
    {
        _pricingExtractsForHotelsInSpecificPeriodDataService = A.Fake<IPricingExtractsForHotelsInSpecificPeriodDataService>();
        var passThroughCurrencyConverter = A.Fake<ICurrencyConverter>();
        _sut = new HotelPricingComparisonService(_pricingExtractsForHotelsInSpecificPeriodDataService, passThroughCurrencyConverter);
        _fixture = AutoFixtureFactory.Instance;
        A.CallTo(() => passThroughCurrencyConverter.ConvertPrice(A<PriceInfo>._, "USD", A<DateOnly>._, A<CancellationToken>._))
            .ReturnsLazily(fakedCall => Task.FromResult(fakedCall.GetArgument<PriceInfo>(0)));
    }

    [Fact]
    public async Task GIVEN_no_extracts_WHEN_GetPricingComparison_THEN_returns_empty_response()
    {
        var result = await _sut.GetPricingComparison(
            [123],
            DateOnly.FromDateTime(DateTime.Today),
            4,
            "USD",
            false,
            TestContext.Current.CancellationToken
        );

        Assert.Empty(result.Prices);
    }

    [Fact]
    public async Task GIVEN_only_current_pricing_and_no_historical_pricing_WHEN_GetPricingComparison_THEN_returns_response_without_difference()
    {
        var arrival = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival).DaysSinceEpoch)
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        var hotelIds = new[] { pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth.OurHotelId };
        var arrivalMonth = DateOnly.FromDateTime(arrival.DateTime);
        A.CallTo(() => _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth, ExtractWindow.ForArrivalMonth(arrivalMonth),
                TestContext.Current.CancellationToken))
            .Returns([pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth]);

        var result = await _sut.GetPricingComparison(
            hotelIds,
            arrivalMonth,
            4,
            "USD",
            false,
            TestContext.Current.CancellationToken
        );

        var price = Assert.Single(result.Prices);
        Assert.Equivalent(
            new PriceRecord
            {
                ArrivalDate = "2020-02-15", 
                Currency = "USD", 
                Price = 90m,
                Hotel = pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth.OurHotelId
            }, price);
    }
    
    [Theory, AutoData]
    public async Task GIVEN_both_current_pricing_and_historical_pricing_WHEN_GetPricingComparison_THEN_returns_response_with_historical_difference_included(int hotelId)
    {
        var arrival = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, hotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival).DaysSinceEpoch)
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        var moreRecentCurrentExtract = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, hotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 10, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival).DaysSinceEpoch)
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 120).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 85).Create()
            ])
            .Create();
        var hotelIds = new[] { hotelId };
        var arrivalMonth = DateOnly.FromDateTime(arrival.DateTime);
        A.CallTo(() => _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth, ExtractWindow.ForArrivalMonth(arrivalMonth),
                TestContext.Current.CancellationToken))
            .Returns([
                moreRecentCurrentExtract,
                pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth
            ]);
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenthFourYearsAgo = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, hotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2016, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival.AddYears(-4)).DaysSinceEpoch)
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 110).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 95).Create()
            ])
            .Create();
        var moreRecentHistoricalExtract = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, hotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2016, 1, 10, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival.AddYears(-4)).DaysSinceEpoch)
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 105).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 80).Create()
            ])
            .Create();
        A.CallTo(() => _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth.AddYears(-4), ExtractWindow.ForArrivalMonth(arrivalMonth.AddYears(-4)),
                TestContext.Current.CancellationToken))
            .Returns([
                moreRecentHistoricalExtract,
                pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenthFourYearsAgo
            ]);

        var result = await _sut.GetPricingComparison(
            hotelIds,
            arrivalMonth,
            4,
            "USD",
            false,
            TestContext.Current.CancellationToken
        );

        var price = Assert.Single(result.Prices);
        Assert.Equivalent(
            new PriceRecord
            {
                ArrivalDate = "2020-02-15", 
                Currency = "USD", 
                Price = 85m,
                Hotel = pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth.OurHotelId,
                Difference = 5m
            }, price);
    }

    [Fact]
    public async Task GIVEN_cancellable_only_WHEN_lower_price_is_not_cancellable_THEN_filters_non_cancellable_prices()
    {
        var arrival = new DateTimeOffset(2020, 3, 20, 0, 0, 0, TimeSpan.Zero);
        var cancellablePrice = _fixture.Build<PriceInfo>()
            .With(price => price.PriceValue, 120m)
            .With(price => price.IsCancellable, true)
            .Create();
        var nonCancellablePrice = _fixture.Build<PriceInfo>()
            .With(price => price.PriceValue, 90m)
            .With(price => price.IsCancellable, false)
            .Create();
        var extract = _fixture.Build<PricingExtractForHotel>()
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 2, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ArrivalDay.From(arrival).DaysSinceEpoch)
            .With(extract => extract.Prices, [cancellablePrice, nonCancellablePrice])
            .Create();
        var hotelIds = new[] { extract.OurHotelId };
        var arrivalMonth = DateOnly.FromDateTime(arrival.DateTime);
        A.CallTo(() => _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth, ExtractWindow.ForArrivalMonth(arrivalMonth),
                TestContext.Current.CancellationToken))
            .Returns([extract]);
        A.CallTo(() => _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth.AddYears(-4), ExtractWindow.ForArrivalMonth(arrivalMonth.AddYears(-4)),
                TestContext.Current.CancellationToken))
            .Returns([]);

        var result = await _sut.GetPricingComparison(
            hotelIds,
            arrivalMonth,
            4,
            "USD",
            true,
            TestContext.Current.CancellationToken
        );

        var price = Assert.Single(result.Prices);
        Assert.Equal(120m, price.Price);
        Assert.Equal("2020-03-20", price.ArrivalDate);
    }
}

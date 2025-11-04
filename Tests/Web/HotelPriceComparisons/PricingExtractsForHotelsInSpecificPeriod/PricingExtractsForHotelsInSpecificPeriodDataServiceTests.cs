using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit3;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using HotelPricingInsights.Controllers.HotelPriceComparison.PricingExtractsForHotelsInSpecificPeriod;
using Tests.Infrastructure;

namespace Tests.Web.HotelPriceComparisons.PricingExtractsForHotelsInSpecificPeriod;

public sealed class PricingExtractsForHotelsInSpecificPeriodDataServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly BigtableEmulatorFixture _bigtableEmulatorFixture;
    private readonly BigtableClient _bigtableClient;
    private PricingExtractsForHotelsInSpecificPeriodDataService _sut = null!;
    private Table _ratesTable = null!;
    private const string RatesTableId = "hotel_rates";
    private const int MariottGhentHotelId = 101;

    public PricingExtractsForHotelsInSpecificPeriodDataServiceTests(BigtableEmulatorFixture bigtableEmulatorFixture)
    {
        _bigtableEmulatorFixture = bigtableEmulatorFixture;
        _bigtableClient = bigtableEmulatorFixture.GetBigtableClient();
        _fixture = new Fixture();
    }

    [Theory, AutoData]
    public async Task GIVEN_no_extracts_WHEN_Get_THEN_returns_empty_collection(IReadOnlyCollection<int> randomHotelIds,
        ExtractWindow extractWindow)
    {
        Assert.Empty(await _sut.Get(randomHotelIds, DateOnly.FromDateTime(DateTime.Today), extractWindow, TestContext.Current.CancellationToken));
    }

    [Theory, AutoData]
    public async Task GIVEN_extracts_for_different_hotel_WHEN_Get_THEN_returns_empty_collection(IReadOnlyCollection<int> randomHotelIds,
        ExtractWindow extractWindow)
    {
        var arrivalDate = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var selectedMonth = new DateOnly(2020, 2, 1);
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ToDayIndex(arrivalDate))
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        await GivenPricingExtract(pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth);

        Assert.Empty(await _sut.Get(randomHotelIds, selectedMonth, extractWindow, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GIVEN_extracts_for_same_hotel_but_extracted_in_too_old_extract_window_WHEN_Get_THEN_returns_empty_collection()
    {
        var arrivalDate = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var selectedMonth = new DateOnly(2020, 2, 1);
        var extractWindow = new ExtractWindow(new DateTimeOffset(2015, 12, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2015, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, MariottGhentHotelId)
            .With(extract => extract.ExtractDate, extractWindow.StartUtc.AddDays(1).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ToDayIndex(arrivalDate))
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        await GivenPricingExtract(pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth);

        Assert.Empty(await _sut.Get(
            [MariottGhentHotelId],
            selectedMonth,
            new ExtractWindow(
                new DateTimeOffset(2019, 12, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2019, 12, 31, 0, 0, 0, TimeSpan.Zero)
            ),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GIVEN_extracts_for_same_hotel_and_extracted_in_extract_window_WHEN_Get_THEN_returns_those_extracts()
    {
        var arrivalDate = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var selectedMonth = new DateOnly(2020, 2, 1);
        var extractWindow = new ExtractWindow(new DateTimeOffset(2019, 12, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2019, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, MariottGhentHotelId)
            .With(extract => extract.ExtractDate, extractWindow.StartUtc.AddDays(1).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, ToDayIndex(arrivalDate))
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        await GivenPricingExtract(pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth);

        var fetchedExtract = Assert.Single(await _sut.Get([MariottGhentHotelId], selectedMonth, extractWindow, TestContext.Current.CancellationToken));
        Assert.Equivalent(pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth, fetchedExtract);
    }

    private async Task GivenPricingExtract(PricingExtractForHotel pricingExtract)
    {
        var extractDate = DateTimeOffset.FromUnixTimeSeconds(pricingExtract.ExtractDate);
        var rowKey = $"{pricingExtract.OurHotelId}#{extractDate:yyyy-MM-dd}#{DayIndexToDateTimeOffset(pricingExtract.ArrivalDate):yyyy-MM-dd}";
        var mutation = Mutations.SetCell(
            familyName: "rates",
            columnQualifier: "payload",
            value: ByteString.CopyFromUtf8(JsonSerializer.Serialize(pricingExtract)),
            version: new BigtableVersion(extractDate.UtcDateTime)
        );

        await _bigtableClient.MutateRowAsync(_ratesTable.TableName, rowKey, mutation);
    }

    private static DateTimeOffset DayIndexToDateTimeOffset(int dayIndex)
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return epoch.AddDays(dayIndex);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        //New table to make sure there is no data leaking across tests
        _ratesTable = await _bigtableEmulatorFixture.CreateTable($"{RatesTableId}-{Guid.NewGuid()}", "rates");
        _sut = new PricingExtractsForHotelsInSpecificPeriodDataService(_bigtableClient, _ratesTable);
    }

    private static int ToDayIndex(DateTimeOffset dateTimeOffset) =>
        (int)(dateTimeOffset.UtcDateTime - DateTime.UnixEpoch).TotalDays;
}
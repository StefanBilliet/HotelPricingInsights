using AutoFixture.Xunit3;
using Google.Cloud.Bigtable.Common.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;
using Tests.Infrastructure;

namespace Tests.Web.HotelPriceComparisons.LatestExtractDatesForHotels;

public class LatestExtractDatesForHotelsDataServiceTests
{
    private readonly BigtableEmulatorFixture _bigtableEmulatorFixture;
    private readonly LatestExtractDatesForHotelsDataService _sut;

    public LatestExtractDatesForHotelsDataServiceTests(BigtableEmulatorFixture bigtableEmulatorFixture)
    {
        _bigtableEmulatorFixture = bigtableEmulatorFixture;
        _sut = new LatestExtractDatesForHotelsDataService(bigtableEmulatorFixture.GetBigtableClient());
    }

    [Theory, AutoData]
    public async Task GIVEN_no_matching_rows_WHEN_Get_THEN_returns_empty_collection(IReadOnlyCollection<string> hotelIds)
    {
        var result = await _sut.Get(hotelIds, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GIVEN_matching_rows_WHEN_Get_THEN_returns_the_matching_latest_extract_dates_for_the_hotels_with_the_provided_ids(string hotelId,
        DateTimeOffset latestExtractDate)
    {
        var latestExtractTable = await _bigtableEmulatorFixture.CreateTable("latest_extract_dates_for_hotels", "meta");
        await GivenExtractDateForHotel(_bigtableEmulatorFixture.GetBigtableClient(), latestExtractTable.TableName, hotelId, latestExtractDate,
            TestContext.Current.CancellationToken);

        var result = await _sut.Get([hotelId], TestContext.Current.CancellationToken);

        var record = Assert.Single(result);
        Assert.Equal(hotelId, record.hotelId);
        Assert.Equal(latestExtractDate.ToUnixTimeMilliseconds(), record.latestExtractDate.ToUnixTimeMilliseconds());
    }

    private static Task<MutateRowResponse> GivenExtractDateForHotel(
        BigtableClient bigtableClient,
        TableName table,
        string hotelId,
        DateTimeOffset extractDate,
        CancellationToken ct)
    {
        var epochMicros = extractDate.ToUniversalTime().ToUnixTimeMilliseconds() * 1_000;

        var mutation = Mutations.SetCell(
            familyName: "meta",
            columnQualifier: new BigtableByteString("extract"),
            value: ByteString.Empty,
            version: new BigtableVersion(epochMicros));

        var mutateRowRequest = new MutateRowRequest
        {
            TableNameAsTableName = table,
            RowKey = ByteString.CopyFromUtf8(hotelId)
        };
        mutateRowRequest.Mutations.Add(mutation);
        return bigtableClient.MutateRowAsync(mutateRowRequest, ct);
    }
}
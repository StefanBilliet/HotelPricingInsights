using System.Text.Json;
using AutoFixture;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;
using Tests.Infrastructure;

namespace Tests;

public sealed class BigtableIntegrationTests
{
    private readonly Fixture _fixture;
    private readonly BigtableEmulatorFixture _bigtableEmulatorFixture;
    private readonly BigtableClient _bigtableClient;
    private const string RatesTableId = "rates";
    private const string MariottGhentHotelId = "H101";

    public BigtableIntegrationTests(BigtableEmulatorFixture bigtableEmulatorFixture)
    {
        _bigtableEmulatorFixture = bigtableEmulatorFixture;
        _bigtableClient = bigtableEmulatorFixture.GetBigtableClient();
        _fixture = new Fixture();
    }

    [Fact]
    public async Task SeedAndQueryLatestExtract_LowestPrice()
    {
        var arrival = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);

        var ratesTable = await _bigtableEmulatorFixture.CreateTable(RatesTableId);

        var pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, MariottGhentHotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, arrival.ToUnixTimeSeconds())
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 100).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 90).Create()
            ])
            .Create();
        var pricingExtractForMariottGhentExtractedOnJanuaryNinthForArrivalOnFebruaryFifteenth = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.OurHotelId, MariottGhentHotelId)
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 9, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, arrival.ToUnixTimeSeconds())
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 120).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 110).Create()
            ])
            .Create();
        var pricingExtractForCompletelyDifferentHotel = _fixture
            .Build<PricingExtractForHotel>()
            .With(extract => extract.ExtractDate, new DateTimeOffset(2020, 1, 9, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds())
            .With(extract => extract.ArrivalDate, arrival.ToUnixTimeSeconds())
            .With(extract => extract.Prices, [
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 120).Create(),
                _fixture.Build<PriceInfo>().With(price => price.PriceValue, 110).Create()
            ])
            .Create();
        await SeedPricingExtract(_bigtableClient, ratesTable, pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth);
        await SeedPricingExtract(_bigtableClient, ratesTable, pricingExtractForMariottGhentExtractedOnJanuaryNinthForArrivalOnFebruaryFifteenth);
        await SeedPricingExtract(_bigtableClient, ratesTable, pricingExtractForCompletelyDifferentHotel);


        // Scan all rows for this hotel+arrival prefix, then pick the row with max extract date
        var pattern = $@"^{MariottGhentHotelId}#\d{{4}}-\d{{2}}-\d{{2}}#{arrival:yyyy-MM-dd}$";
        var rows = _bigtableClient.ReadRows(new ReadRowsRequest
        {
            TableName = ratesTable.Name,
            Filter = RowFilters.RowKeyRegex(pattern)
        });

        string? latestRowKey = null;
        Row? latestRow = null;

        await foreach (var row in rows)
        {
            if (latestRowKey == null || string.Compare(row.Key.ToStringUtf8(), latestRowKey, StringComparison.Ordinal) > 0)
            {
                latestRowKey = row.Key.ToStringUtf8();
                latestRow = row;
            }
        }

        Assert.NotNull(latestRow);

        var cell = latestRow.Families
            .Single(family => family.Name == "rates")
            .Columns.Single(column => column.Qualifier.ToStringUtf8() == "payload")
            .Cells.Single();

        var json = cell.Value.ToStringUtf8();
        var pricingExtractForHotel = JsonSerializer.Deserialize<PricingExtractForHotel>(json);
        var lowestPrice = pricingExtractForHotel.Prices.Min(price => price.PriceValue);

        Assert.Equal(110.00m, lowestPrice);
    }

    private static async Task SeedPricingExtract(BigtableClient bigtableClient, Table table, PricingExtractForHotel pricingExtract)
    {
        var rowKey = $"{pricingExtract.OurHotelId}#{DateTimeOffset.FromUnixTimeSeconds(pricingExtract.ExtractDate):yyyy-MM-dd}#{DateTimeOffset.FromUnixTimeSeconds(pricingExtract.ArrivalDate):yyyy-MM-dd}";
        var mutation = Mutations.SetCell(
            familyName: "rates",
            columnQualifier: "payload",
            value: ByteString.CopyFromUtf8(JsonSerializer.Serialize(pricingExtract))
        );

        await bigtableClient.MutateRowAsync(table.TableName, rowKey, mutation);
    }
}
using System.Text.Json;
using AutoFixture;
using Google.Api.Gax;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;
using Testcontainers.Bigtable;

namespace Tests;

public sealed class BigtableIntegrationTests : IAsyncLifetime
{
    private readonly BigtableContainer _bigtableContainer;
    private readonly Fixture _fixture;
    private const string InstanceId = "test-instance";
    private const string RatesTableId = "rates";
    private const string MariottGhentHotelId = "H101";

    public BigtableIntegrationTests()
    {
        _bigtableContainer = new BigtableBuilder().Build();
        _fixture = new Fixture();
    }

    public async ValueTask InitializeAsync()
    {
        await _bigtableContainer.StartAsync();

        // Set environment variable for emulator
        Environment.SetEnvironmentVariable("BIGTABLE_EMULATOR_HOST", _bigtableContainer.GetEmulatorEndpoint());
    }

    public async ValueTask DisposeAsync()
    {
        await _bigtableContainer.DisposeAsync();
    }

    [Fact]
    public async Task SeedAndQueryLatestExtract_LowestPrice()
    {
        var arrival = new DateTimeOffset(2020, 2, 15, 0, 0, 0, TimeSpan.Zero);

        // Build admin + data clients that must talk to the emulator
        var admin = await new BigtableTableAdminClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync(TestContext.Current.CancellationToken);

        var bigtableClient = await new BigtableClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync(TestContext.Current.CancellationToken);

        var ratesTable = await admin.CreateTableAsync(
            parent: InstanceId,
            tableId: RatesTableId,
            table: new Table
            {
                ColumnFamilies = { { RatesTableId, new ColumnFamily() } }
            });

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
        await SeedPricingExtract(bigtableClient, ratesTable, pricingExtractForMariottGhentExtractedOnJanuaryFirstForArrivalOnFebruaryFifteenth);
        await SeedPricingExtract(bigtableClient, ratesTable, pricingExtractForMariottGhentExtractedOnJanuaryNinthForArrivalOnFebruaryFifteenth);
        await SeedPricingExtract(bigtableClient, ratesTable, pricingExtractForCompletelyDifferentHotel);


        // Scan all rows for this hotel+arrival prefix, then pick the row with max extract date
        var pattern = $@"^{MariottGhentHotelId}#\d{{4}}-\d{{2}}-\d{{2}}#{arrival:yyyy-MM-dd}$";
        var rows = bigtableClient.ReadRows(new ReadRowsRequest
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

    private async Task SeedPricingExtract(BigtableClient bigtableClient, Table table, PricingExtractForHotel pricingExtract)
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
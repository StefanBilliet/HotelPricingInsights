using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;
using Testcontainers.Bigtable;

namespace Tests;

public sealed class BigtableIntegrationTests : IAsyncLifetime
{
    private readonly BigtableContainer _bigtableContainer;
    private const string InstanceId = "test-instance";
    private const string RatesTableId = "rates";

    public BigtableIntegrationTests()
    {
        _bigtableContainer = new BigtableBuilder()
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _bigtableContainer.StartAsync();
        
        // Set environment variable for emulator
        Environment.SetEnvironmentVariable("BIGTABLE_EMULATOR_HOST", _bigtableContainer.GetEmulatorEndpoint());
    }

    public async Task DisposeAsync()
    {
        // _adminClient?.Dispose();
        await _bigtableContainer.DisposeAsync();
    }
    
    [Fact]
    public async Task SeedAndQueryLatestExtract_LowestPrice()
    {
        var hotelId = "H101";
        var arrival = new DateOnly(2025, 06, 15);
        
        // Build admin + data clients that must talk to the emulator
        var admin = await new BigtableTableAdminClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync();

        var bigtableClient = await new BigtableClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync();
        
        var ratesTable = await admin.CreateTableAsync(
            parent: InstanceId,
            tableId: RatesTableId,
            table: new Table
            {
                ColumnFamilies = { { RatesTableId, new ColumnFamily() } }
            });

        // Two extracts on different days for the same arrival date
        await SeedRateSnapshotAsync(bigtableClient, ratesTable, hotelId, arrival, new DateOnly(2025, 05, 01),
            /* JSON payload */ """
            { "offers":[
                { "name":"BAR", "amount":149.00, "currency":"EUR" },
                { "name":"NonRefundable", "amount":129.99, "currency":"EUR" }
            ] }
            """);

        await SeedRateSnapshotAsync(bigtableClient, ratesTable, hotelId, arrival, new DateOnly(2025, 05, 02),
            """
            { "offers":[
                { "name":"BAR", "amount":139.00, "currency":"EUR" },
                { "name":"Member", "amount":119.00, "currency":"EUR" }
            ] }
            """);

        // Scan all rows for this hotel+arrival prefix, then pick the row with max extract date
        var pattern = $@"^{hotelId}#\d{{4}}-\d{{2}}-\d{{2}}#{arrival:yyyy-MM-dd}$";
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
            .Single(f => f.Name == "rates")
            .Columns.Single(c => c.Qualifier.ToStringUtf8() == "payload")
            .Cells.Single();

        var json = cell.Value.ToStringUtf8();
        using var doc = JsonDocument.Parse(json);
        var lowest = doc.RootElement.GetProperty("offers")
            .EnumerateArray()
            .Select(o => o.GetProperty("amount").GetDecimal())
            .Min();

        Assert.Equal(119.00m, lowest);
    }

    private async Task SeedRateSnapshotAsync(BigtableClient bigtableClient, Table table, string hotelId, DateOnly arrival, DateOnly extract, string json)
    {
        var rowKey = $"{hotelId}#{extract:yyyy-MM-dd}#{arrival:yyyy-MM-dd}";
        var mutation = Mutations.SetCell(
            familyName: "rates",
            columnQualifier: "payload",
            value: ByteString.CopyFromUtf8(json));

        await bigtableClient.MutateRowAsync(table.TableName, rowKey, mutation);
    }
}
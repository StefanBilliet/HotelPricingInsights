using Google.Api.Gax;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.V2;
using Testcontainers.Bigtable;

namespace Tests;

public sealed class BigtableEmulatorFixture : IAsyncLifetime
{
    private readonly BigtableContainer _bigtableContainer;
    private BigtableTableAdminClient _admin;
    private BigtableClient _bigtableClient;
    private const string InstanceId = "test-instance";

    public BigtableEmulatorFixture()
    {
        _bigtableContainer = new BigtableBuilder().Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _bigtableContainer.StartAsync();
        
        // Set environment variable for emulator
        Environment.SetEnvironmentVariable("BIGTABLE_EMULATOR_HOST", _bigtableContainer.GetEmulatorEndpoint());
        
        _admin = await new BigtableTableAdminClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync(TestContext.Current.CancellationToken);
        
        _bigtableClient = await new BigtableClientBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.BuildAsync(TestContext.Current.CancellationToken);
    }

    public string GetEmulatorEndpoint() => _bigtableContainer.GetEmulatorEndpoint();

    public BigtableClient GetBigtableClient() => _bigtableClient;

    public async ValueTask DisposeAsync()
    {
        await _bigtableContainer.DisposeAsync();
    }

    public Task<Table> CreateTable(string tableId)
    {
        return _admin.CreateTableAsync(
            parent: InstanceId,
            tableId: tableId,
            table: new Table
            {
                ColumnFamilies = { { tableId, new ColumnFamily() } }
            });
    }
}
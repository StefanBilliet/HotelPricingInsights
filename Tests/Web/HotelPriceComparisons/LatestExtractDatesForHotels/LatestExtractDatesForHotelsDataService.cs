using Google.Api.Gax;
using Google.Cloud.Bigtable.Common.V2;
using Google.Cloud.Bigtable.V2;

namespace Tests.Web.HotelPriceComparisons.LatestExtractDatesForHotels;

public class LatestExtractDatesForHotelsDataService
{
    private readonly BigtableClient _bigtableClient;

    public LatestExtractDatesForHotelsDataService(BigtableClient bigtableClient)
    {
        _bigtableClient = bigtableClient;
    }

    public async Task<IReadOnlyCollection<(string hotelId, DateTimeOffset latestExtractDate)>> Get(IReadOnlyCollection<string> hotelIds,
        CancellationToken cancellationToken)
    {
        var readRowsRequest = new ReadRowsRequest
        {
            TableNameAsTableName = TableName.FromUnparsed(new UnparsedResourceName("test-instance/tables/latest_extract_dates_for_hotels")),
            Rows = RowSet.FromRowKeys(hotelIds.Select(id => new BigtableByteString(id.ToString()))),
            Filter = RowFilters.Chain(
                RowFilters.FamilyNameExact("meta"),
                RowFilters.ColumnQualifierExact(new BigtableByteString("extract")),
                RowFilters.CellsPerColumnLimit(1)
            )
        };

        return await _bigtableClient.ReadRows(readRowsRequest)
            .Select(row =>
            {
                var cell = row.Families[0].Columns[0].Cells[0];
                var milliseconds = cell.TimestampMicros / 1_000_000L;
                return (row.Key.ToStringUtf8(), DateTimeOffset.FromUnixTimeMilliseconds(milliseconds));
            })
            .ToArrayAsync(cancellationToken);
    }
}
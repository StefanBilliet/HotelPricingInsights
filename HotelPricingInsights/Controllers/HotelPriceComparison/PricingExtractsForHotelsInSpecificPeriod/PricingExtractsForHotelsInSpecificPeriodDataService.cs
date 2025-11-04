using System.Text.Json;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.Common.V2;
using Google.Cloud.Bigtable.V2;
using Google.Protobuf;

namespace HotelPricingInsights.Controllers.HotelPriceComparison.PricingExtractsForHotelsInSpecificPeriod;

public interface IPricingExtractsForHotelsInSpecificPeriodDataService
{
    Task<IReadOnlyList<PricingExtractForHotel>> Get(IReadOnlyCollection<int> hotelIds,
        DateOnly monthOfArrivalDates,
        ExtractWindow extractWindow,
        CancellationToken cancellationToken);
}

public class PricingExtractsForHotelsInSpecificPeriodDataService : IPricingExtractsForHotelsInSpecificPeriodDataService
{
    private readonly BigtableClient _bigtableClient;
    private readonly Table _ratesTable;

    public PricingExtractsForHotelsInSpecificPeriodDataService(BigtableClient bigtableClient, Table ratesTable)
    {
        _bigtableClient = bigtableClient;
        _ratesTable = ratesTable;
    }

    public async Task<IReadOnlyList<PricingExtractForHotel>> Get(IReadOnlyCollection<int> hotelIds,
        DateOnly monthOfArrivalDates,
        ExtractWindow extractWindow,
        CancellationToken cancellationToken)
    {
        var monthStartIndex = ArrivalDay.FromDateOnly(monthOfArrivalDates).DaysSinceEpoch;
        var monthEndIndex = ArrivalDay.FromDateOnly(monthOfArrivalDates.AddMonths(1)).DaysSinceEpoch;

        var ranges = new RowSet();
        foreach (var id in hotelIds)
        {
            var hotelRowKeyPrefix = $"{id}#";
            ranges.RowRanges.Add(new RowRange
            {
                StartKeyClosed = ByteString.CopyFromUtf8(hotelRowKeyPrefix),
                EndKeyOpen = ByteString.CopyFromUtf8($"{hotelRowKeyPrefix}\xff") // max byte to capture the prefix
            });
        }

        var readRowsRequest = new ReadRowsRequest
        {
            TableNameAsTableName = _ratesTable.TableName,
            Rows = ranges,
            Filter = RowFilters.Chain(
                RowFilters.FamilyNameExact("rates"),
                RowFilters.ColumnQualifierExact(new BigtableByteString("payload")),
                RowFilters.TimestampRange(extractWindow.StartUtc.UtcDateTime, extractWindow.EndUtcExclusive.UtcDateTime)
            )
        };

        var result = new List<PricingExtractForHotel>();

        await foreach (var row in _bigtableClient.ReadRows(readRowsRequest).WithCancellation(cancellationToken))
        {
            var cell = row.Families[0].Columns[0].Cells[0];

            var pricingExtractForHotel = JsonSerializer.Deserialize<PricingExtractForHotel>(cell.Value.ToStringUtf8());

            if (pricingExtractForHotel == null || pricingExtractForHotel.ArrivalDate < monthStartIndex || pricingExtractForHotel.ArrivalDate >= monthEndIndex)
            {
                continue;
            }

            result.Add(pricingExtractForHotel);
        }

        return result;
    }
}
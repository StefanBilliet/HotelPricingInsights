namespace HotelPricingInsights.Controllers.HotelPriceComparison;

public sealed record ExtractWindow(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtcExclusive
)
{
    public static ExtractWindow ForArrivalMonth(DateOnly arrivalMonth)
    {
        var extractMonth = arrivalMonth.AddMonths(-1);
        var start = new DateTimeOffset(extractMonth.Year, extractMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        
        return new ExtractWindow(start, end);
    }
}
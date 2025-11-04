namespace HotelPricingInsights.Controllers.HotelPriceComparison;

public record ArrivalDay
{
    public int DaysSinceEpoch { get; }

    public ArrivalDay(int daysSinceEpoch)
    {
        DaysSinceEpoch = daysSinceEpoch;
    }

    public DateOnly ToDateOnly() =>
        DateOnly.FromDateTime(DateTimeOffset.UnixEpoch.AddDays(DaysSinceEpoch).UtcDateTime);

    public static ArrivalDay FromDateOnly(DateOnly date) => new((int)(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalDays);
    
    public static ArrivalDay From(DateTimeOffset dateTimeOffset)
    {
        var utcDateTime = dateTimeOffset.UtcDateTime;
        return new ArrivalDay((int)(utcDateTime - DateTime.UnixEpoch).TotalDays);
    }
}
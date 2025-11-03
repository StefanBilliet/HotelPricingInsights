using System.Globalization;

namespace Tests.PricingExtractsForHotelsInSpecificPeriod;

public readonly struct ArrivalDay
{
    public int DayIndex { get; }

    public ArrivalDay(int dayIndex)
    {
        DayIndex = dayIndex;
    }

    public DateOnly ToDateOnly() =>
        DateOnly.FromDayNumber(DayIndex);

    public string ToKeyString() =>
        ToDateOnly().ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public static ArrivalDay FromDateOnly(DateOnly date) =>
        new ArrivalDay((int)(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalDays);
}
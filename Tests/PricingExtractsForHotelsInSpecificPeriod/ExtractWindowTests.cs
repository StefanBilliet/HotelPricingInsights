namespace Tests.PricingExtractsForHotelsInSpecificPeriod;

public class ExtractWindowTests
{
    [Fact]
    public void GIVEN_february_arrival_month_WHEN_ForArrivalMonth_THEN_returns_january_extract_window()
    {
        var arrivalMonth = new DateOnly(2025, 2, 1);

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), window.EndUtcExclusive);
    }

    [Fact]
    public void GIVEN_january_arrival_month_WHEN_ForArrivalMonth_THEN_returns_december_previous_year_extract_window()
    {
        var arrivalMonth = new DateOnly(2025, 1, 1);

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        Assert.Equal(new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), window.EndUtcExclusive);
    }

    [Fact]
    public void GIVEN_march_arrival_month_WHEN_ForArrivalMonth_THEN_returns_february_extract_window()
    {
        var arrivalMonth = new DateOnly(2024, 3, 1); // Leap year

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        Assert.Equal(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero), window.EndUtcExclusive);
    }

    [Fact]
    public void GIVEN_december_arrival_month_WHEN_ForArrivalMonth_THEN_returns_november_extract_window()
    {
        var arrivalMonth = new DateOnly(2025, 12, 1);

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        Assert.Equal(new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero), window.EndUtcExclusive);
    }

    [Fact]
    public void GIVEN_arrival_month_with_specific_day_WHEN_ForArrivalMonth_THEN_ignores_day_and_uses_first_of_month()
    {
        var arrivalMonth = new DateOnly(2025, 5, 15);

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        Assert.Equal(new DateTimeOffset(2025, 4, 1, 0, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero), window.EndUtcExclusive);
    }

    [Fact]
    public void GIVEN_extract_window_WHEN_constructed_THEN_end_is_exclusive()
    {
        var arrivalMonth = new DateOnly(2025, 2, 1);

        var window = ExtractWindow.ForArrivalMonth(arrivalMonth);

        // The end should be exactly the start of the arrival month
        // Meaning extracts made at 2025-02-01 00:00:00 are NOT included
        Assert.Equal(window.EndUtcExclusive, new DateTimeOffset(arrivalMonth, TimeOnly.MinValue, TimeSpan.Zero));
    }
}
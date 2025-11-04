namespace HotelPricingInsights.Controllers;

public sealed class PricingComparisonRequest
{
    public required string Month { get; init; }

    public required string Currency { get; init; }

    public required int[] Hotels { get; init; }

    public required int YearsAgo { get; init; }

    public bool? Cancellable { get; init; } = true;
}

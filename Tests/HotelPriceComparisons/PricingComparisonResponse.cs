using System.Text.Json.Serialization;

namespace Tests.HotelPriceComparisons;

public record PricingComparisonResponse
{
    [JsonPropertyName("prices")]
    public required IReadOnlyCollection<PriceRecord> Prices { get; init; }
}
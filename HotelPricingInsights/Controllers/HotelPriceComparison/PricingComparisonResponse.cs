using System.Text.Json.Serialization;

namespace HotelPricingInsights.Controllers.HotelPriceComparison;

public record PricingComparisonResponse
{
    [JsonPropertyName("prices")]
    public required IReadOnlyCollection<PriceRecord> Prices { get; init; }
}
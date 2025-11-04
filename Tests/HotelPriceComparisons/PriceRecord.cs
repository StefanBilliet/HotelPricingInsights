using System.Text.Json.Serialization;

namespace Tests.HotelPriceComparisons;

public record PriceRecord
{
    [JsonPropertyName("hotel")]
    public required string Hotel { get; init; }
    
    [JsonPropertyName("price")]
    public required decimal Price { get; init; }
    
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }
    
    [JsonPropertyName("difference")]
    public decimal? Difference { get; init; }
    
    [JsonPropertyName("arrival_date")]
    public required string ArrivalDate { get; init; } // "yyyy-MM-dd" format
}
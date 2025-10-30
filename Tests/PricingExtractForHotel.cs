namespace Tests;

public record PricingExtractForHotel(
    int ExtractDate,
    long ExtractDateTimeUtc,
    int ArrivalDate,
    int LengthOfStay,
    int OnlineTravelAgencyId,
    string OurHotelId,
    string PointOfSale,
    IReadOnlyCollection<PriceInfo> Prices
);

public record PriceInfo(
    IReadOnlyCollection<BedOption> BedOptions,
    decimal BreakfastCost,
    int CancellationDeadlineDateTimeLocal,
    int CancellationPolicyDays,
    int CityTaxIncl,
    string Currency,
    bool IsBestAvailableRate,
    bool IsCancellable,
    int MaxPersons,
    int MealTypeIncluded,
    int OtherTaxesIncl,
    string PriceId,
    int PriceValue,
    string RoomName,
    int VatIncl
);

public record BedOption(
    bool King
);
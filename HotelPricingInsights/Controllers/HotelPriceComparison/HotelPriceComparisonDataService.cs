using System.Runtime.CompilerServices;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyConversion;
using HotelPricingInsights.Controllers.HotelPriceComparison.PricingExtractsForHotelsInSpecificPeriod;

namespace HotelPricingInsights.Controllers.HotelPriceComparison;

public interface IHotelPricingComparisonService
{
    Task<PricingComparisonResponse> GetPricingComparison(int[] hotelIds,
        DateOnly arrivalMonth,
        int yearsAgo,
        string targetCurrency,
        bool? cancellableOnly,
        CancellationToken cancellationToken);
}

public class HotelPricingComparisonService : IHotelPricingComparisonService
{
    private readonly ICurrencyConverter _currencyConverter;
    private readonly IPricingExtractsForHotelsInSpecificPeriodDataService _pricingExtractsForHotelsInSpecificPeriodDataService;
    
    public HotelPricingComparisonService(IPricingExtractsForHotelsInSpecificPeriodDataService pricingExtractsForHotelsInSpecificPeriodDataService,
        ICurrencyConverter currencyConverter
    )
    {
        _pricingExtractsForHotelsInSpecificPeriodDataService = pricingExtractsForHotelsInSpecificPeriodDataService;
        _currencyConverter = currencyConverter;
    }

    public async Task<PricingComparisonResponse> GetPricingComparison(int[] hotelIds,
        DateOnly arrivalMonth,
        int yearsAgo,
        string targetCurrency,
        bool? cancellableOnly,
        CancellationToken cancellationToken)
    {
        var currentWindow = ExtractWindow.ForArrivalMonth(arrivalMonth);
        var historicalMonth = arrivalMonth.AddYears(-yearsAgo);
        var historicalWindow = ExtractWindow.ForArrivalMonth(historicalMonth);

        // Fetch data in parallel
        var currentTask = _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, arrivalMonth, currentWindow, cancellationToken);
        var historicalTask = _pricingExtractsForHotelsInSpecificPeriodDataService.Get(hotelIds, historicalMonth, historicalWindow, cancellationToken);

        await Task.WhenAll(currentTask, historicalTask);

        // Process extracts
        var currentPrices = await GetLowestPricePerHotelAndArrivalDate(currentTask.Result, arrivalMonth, targetCurrency, cancellableOnly, cancellationToken);
        var historicalPrices = await GetLowestPricePerHotelAndArrivalDate(historicalTask.Result, historicalMonth, targetCurrency, cancellableOnly, cancellationToken);

        return new PricingComparisonResponse
        {
            Prices = BuildPriceRecords(currentPrices, historicalPrices, yearsAgo, targetCurrency)
        };
    }

    private readonly record struct HotelArrivalKey(int HotelId, ArrivalDay ArrivalDay);

    private readonly record struct ExtractPriceSnapshot(decimal Price, int ExtractDate);

    private async Task<Dictionary<HotelArrivalKey, decimal>> GetLowestPricePerHotelAndArrivalDate(
        IReadOnlyList<PricingExtractForHotel> extracts,
        DateOnly monthAnchor,
        string targetCurrency,
        bool? cancellableOnly,
        CancellationToken cancellationToken)
    {
        var pricesByArrival = new Dictionary<HotelArrivalKey, ExtractPriceSnapshot>();

        var extractsWithNormalisedPrices = await ExtractsWithNormalisedPrices(extracts, monthAnchor, cancellableOnly, cancellationToken);

        foreach (var extract in extractsWithNormalisedPrices.Where(extract => extract.Prices.Count > 0))
        {
            var lowestPriceInTarget = extract.Prices.OrderBy(price => price.PriceValue).First();

            var key = new HotelArrivalKey(extract.OurHotelId, new ArrivalDay(extract.ArrivalDate));

            // Keep the most recent extract (highest ExtractDate)
            if (pricesByArrival.TryGetValue(key, out var currentEntry) && extract.ExtractDate <= currentEntry.ExtractDate)
            {
                continue;
            }

            var priceInRequestedTargetCurrency = await _currencyConverter.ConvertPrice(lowestPriceInTarget, targetCurrency, monthAnchor, cancellationToken);

            if (priceInRequestedTargetCurrency == null)
            {
                continue;
            }

            pricesByArrival[key] = new ExtractPriceSnapshot(priceInRequestedTargetCurrency.PriceValue, extract.ExtractDate);
        }

        return pricesByArrival.ToDictionary(priceEntry => priceEntry.Key, priceEntry => priceEntry.Value.Price);
    }

    private async Task<IReadOnlyCollection<PricingExtractForHotel>> ExtractsWithNormalisedPrices(IReadOnlyCollection<PricingExtractForHotel> extractPrices,
        DateOnly monthAnchor,
        bool? cancellableOnly,
        CancellationToken cancellationToken)
    {
        var extractsWithNormalisedPrices = new List<PricingExtractForHotel>();
        foreach (var extract in extractPrices)
        {
            var normalisedPrices = await NormalisePrices(extract.Prices, monthAnchor, cancellableOnly, cancellationToken)
                .ToArrayAsync(cancellationToken: cancellationToken);
            extractsWithNormalisedPrices.Add(extract with { Prices = normalisedPrices });
        }

        return extractsWithNormalisedPrices;
    }

    private async IAsyncEnumerable<PriceInfo> NormalisePrices(IReadOnlyCollection<PriceInfo> extractPrices,
        DateOnly monthAnchor,
        bool? cancellableOnly,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var price in extractPrices)
        {
            if (cancellableOnly.GetValueOrDefault() && !price.IsCancellable)
            {
                continue;
            }
            var normalisedPrice = await _currencyConverter.ConvertPrice(price, "USD", monthAnchor, cancellationToken);
            if (normalisedPrice == null)
            {
                continue;
            }

            yield return normalisedPrice;
        }
    }

    private static IReadOnlyCollection<PriceRecord> BuildPriceRecords(
        Dictionary<HotelArrivalKey, decimal> currentPrices,
        Dictionary<HotelArrivalKey, decimal> historicalPrices,
        int yearsAgo,
        string currency)
    {
        return currentPrices.Select(tuple =>
            {
                var key = tuple.Key;
                var currentPrice = tuple.Value;
                var arrivalDate = key.ArrivalDay.ToDateOnly();

                decimal? difference = null;
                var historicalArrivalDate = arrivalDate.AddYears(-yearsAgo);
                var historicalKey = new HotelArrivalKey(key.HotelId, ArrivalDay.FromDateOnly(historicalArrivalDate));

                if (historicalPrices.TryGetValue(historicalKey, out var historicalPrice))
                {
                    difference = currentPrice - historicalPrice;
                }

                return new PriceRecord
                {
                    Hotel = key.HotelId,
                    Price = currentPrice,
                    Currency = currency,
                    Difference = difference,
                    ArrivalDate = arrivalDate.ToString("yyyy-MM-dd")
                };
            })
            .OrderBy(p => p.Hotel)
            .ThenBy(p => p.ArrivalDate)
            .ToList();
    }
}

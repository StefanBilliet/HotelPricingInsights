using System.Data;
using FluentValidation;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.Common.V2;
using Google.Cloud.Bigtable.V2;
using HotelPricingInsights.Controllers;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyConversion;
using HotelPricingInsights.Controllers.HotelPriceComparison.CurrencyExchangeRates;
using HotelPricingInsights.Controllers.HotelPriceComparison.PricingExtractsForHotelsInSpecificPeriod;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;

namespace HotelPricingInsights;

public static class ServiceCollectionExtensions
{
    public static void AddPricingComparisonServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<AsyncCachePolicy<CurrencyExchangeRate?>>(sp =>
        {
            var cacheProvider = new MemoryCacheProvider(sp.GetRequiredService<IMemoryCache>());
            return Policy.CacheAsync<CurrencyExchangeRate?>(cacheProvider, TimeSpan.FromMinutes(10));
        });

        services.AddScoped<IHotelPricingComparisonService, HotelPricingComparisonService>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();
        services.AddScoped<MonthAnchoredCurrencyExchangeRatesDataService>();
        services.AddScoped<ICurrencyExchangeRatesDataService>(serviceProvider =>
        {
            var inner = serviceProvider.GetRequiredService<MonthAnchoredCurrencyExchangeRatesDataService>();
            var cachePolicy = serviceProvider.GetRequiredService<AsyncCachePolicy<CurrencyExchangeRate?>>();
            return new CachingCurrencyExchangeRatesDataService(inner, cachePolicy);
        });
        services.AddScoped<IPricingExtractsForHotelsInSpecificPeriodDataService, PricingExtractsForHotelsInSpecificPeriodDataService>();
        services.AddTransient<IValidator<PricingComparisonRequest>, PricingComparisonRequestValidator>();
        services.AddTransient<PricingComparisonEndpoint>();

        services.TryAddSingleton<BigtableClient>(_ => throw new InvalidOperationException("Configure a BigtableClient instance via dependency injection."));
        services.TryAddSingleton<Table>(_ => new Table { Name = TableName.FromProjectInstanceTable("project", "instance", "pricing").ToString() });
        services.TryAddSingleton<Func<IDbConnection>>(_ => throw new InvalidOperationException("Configure a database connection factory via dependency injection."));
    }
}

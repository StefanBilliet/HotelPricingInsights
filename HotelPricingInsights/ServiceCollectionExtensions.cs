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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelPricingInsights;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricingComparisonServices(this IServiceCollection services)
    {
        services.AddScoped<IHotelPricingComparisonService, HotelPricingComparisonService>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();
        services.AddScoped<ICurrencyExchangeRatesDataService, MonthAnchoredCurrencyExchangeRatesDataService>();
        services.AddScoped<IPricingExtractsForHotelsInSpecificPeriodDataService, PricingExtractsForHotelsInSpecificPeriodDataService>();
        services.AddTransient<IValidator<PricingComparisonRequest>, PricingComparisonRequestValidator>();
        services.AddTransient<PricingComparisonController>();

        services.TryAddSingleton<BigtableClient>(_ => throw new InvalidOperationException("Configure a BigtableClient instance via dependency injection."));
        services.TryAddSingleton<Table>(_ => new Table { Name = TableName.FromProjectInstanceTable("project", "instance", "pricing").ToString() });
        services.TryAddSingleton<Func<IDbConnection>>(_ => throw new InvalidOperationException("Configure a database connection factory via dependency injection."));

        return services;
    }
}

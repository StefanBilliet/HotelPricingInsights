using System.Data;
using AutoFixture;
using Dapper;
using Tests.Infrastructure;

namespace Tests.CurrencyExchangeRates;

public class MonthAnchoredCurrencyExchangeRatesDataServiceTests
{
    private readonly MonthAnchoredCurrencyExchangeRatesDataService _sut;
    private readonly Func<IDbConnection> _dbConnectionFactory;
    private readonly Fixture _fixture;

    public MonthAnchoredCurrencyExchangeRatesDataServiceTests(PostgresFixture postgresFixture)
    {
        _fixture = new Fixture();
        //Otherwise AutoFixture will try to generate DateOnly with invalid values
        _fixture.Customize<DateOnly>(customization => customization.FromFactory<DateTime>(dateTime => DateOnly.FromDateTime(dateTime.Date)));
        _dbConnectionFactory = postgresFixture.GetConnectionFactory();
        _sut = new MonthAnchoredCurrencyExchangeRatesDataService(_dbConnectionFactory);
    }

    [Fact, AutoRollback]
    public async Task GIVEN_no_extracted_currency_exchange_rates_WHEN_Get_THEN_returns_null()
    {
        var currencyExchangeRate = _fixture.Create<CurrencyExchangeRate>();
        
        var exchangeRate = await _sut.GetForCurrency(currencyExchangeRate.Currency, currencyExchangeRate.ExtractDate, TestContext.Current.CancellationToken);
        
        Assert.Null(exchangeRate);
    }
    
    [Fact, AutoRollback]
    public async Task GIVEN_no_matching_extracted_currency_exchange_rate_WHEN_Get_THEN_returns_null()
    {
        var currencyExchangeRate = _fixture.Create<CurrencyExchangeRate>();
        await GivenCurrencyExchangeRates([currencyExchangeRate]);
        
        var exchangeRate = await _sut.GetForCurrency("BTC", currencyExchangeRate.ExtractDate, TestContext.Current.CancellationToken);
        
        Assert.Null(exchangeRate);
    }
    
    [Fact, AutoRollback]
    public async Task GIVEN_matching_extracted_currency_exchange_rate_but_different_period_WHEN_Get_THEN_returns_null()
    {
        var currencyExchangeRate = _fixture.Create<CurrencyExchangeRate>();
        await GivenCurrencyExchangeRates([currencyExchangeRate]);
        
        var exchangeRate = await _sut.GetForCurrency(currencyExchangeRate.Currency, new DateOnly(1,1,1), TestContext.Current.CancellationToken);
        
        Assert.Null(exchangeRate);
    }
    
    [Fact, AutoRollback]
    public async Task GIVEN_matching_extracted_currency_exchange_rate_WHEN_Get_THEN_matching_returns_currency_exchange_rate()
    {
        var currencyExchangeRate = _fixture.Create<CurrencyExchangeRate>();
        await GivenCurrencyExchangeRates([currencyExchangeRate]);

        var exchangeRate = await _sut.GetForCurrency(currencyExchangeRate.Currency, currencyExchangeRate.ExtractDate, TestContext.Current.CancellationToken);
        
        Assert.Equivalent(currencyExchangeRate, exchangeRate);
    }

    private async Task GivenCurrencyExchangeRates(IReadOnlyCollection<CurrencyExchangeRate> currencyExchangeRates)
    {
        const string insertScript = """
                                    INSERT INTO exchange_rates (currency, extract_date, usd_conversion_rate, last_update)
                                    VALUES (@Currency, @ExtractDate, @UsdConversionRate, @LastUpdate);
                                    """;

        var parameters = currencyExchangeRates.Select(currencyExchangeRate => new
        {
            currencyExchangeRate.Currency,
            currencyExchangeRate.ExtractDate,
            currencyExchangeRate.UsdConversionRate,
            LastUpdate = DateTime.UtcNow
        });
        using var db = _dbConnectionFactory();
        await db.ExecuteAsync(new CommandDefinition(insertScript, parameters, cancellationToken: TestContext.Current.CancellationToken));
    }
}
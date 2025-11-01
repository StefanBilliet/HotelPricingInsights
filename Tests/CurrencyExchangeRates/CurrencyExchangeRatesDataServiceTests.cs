using System.Data;
using AutoFixture;
using Dapper;
using Tests.Infrastructure;

namespace Tests.CurrencyExchangeRates;

public class CurrencyExchangeRatesDataServiceTests
{
    private readonly CurrencyExchangeRatesDataService _sut;
    private readonly Func<IDbConnection> _dbConnectionFactory;
    private readonly Fixture _fixture;

    public CurrencyExchangeRatesDataServiceTests(PostgresFixture postgresFixture)
    {
        _fixture = new Fixture();
        //Otherwise AutoFixture will try to generate DateOnly with invalid values
        _fixture.Customize<DateOnly>(customization => customization.FromFactory<DateTime>(dateTime => DateOnly.FromDateTime(dateTime.Date)));
        _dbConnectionFactory = postgresFixture.GetConnectionFactory();
        _sut = new CurrencyExchangeRatesDataService(_dbConnectionFactory);
    }

    [Fact, AutoRollback]
    public async Task GIVEN_no_extracted_currency_exchange_rates_WHEN_Get_THEN_returns_empty_collection()
    {
        Assert.Empty(await _sut.Get(TestContext.Current.CancellationToken));
    }
    
    [Fact, AutoRollback]
    public async Task GIVEN_extracted_currency_exchange_rates_WHEN_Get_THEN_returns_all_exchange_rates()
    {
        var currencyExchangeRates = _fixture.CreateMany<CurrencyExchangeRate>().ToArray();
        await GivenCurrencyExchangeRates(currencyExchangeRates);

        var result = await _sut.Get(TestContext.Current.CancellationToken);
        
        Assert.Equivalent(currencyExchangeRates, result);
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
using System.Data;
using Dapper;

namespace Tests.CurrencyExchangeRates;

public class CurrencyExchangeRatesDataService
{
    private readonly Func<IDbConnection> _dbConnectionFactory;

    public CurrencyExchangeRatesDataService(Func<IDbConnection> dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IReadOnlyCollection<CurrencyExchangeRate>> Get(CancellationToken cancellationToken)
    {
        using var db = _dbConnectionFactory();

        var commandDefinition = new CommandDefinition(commandText: 
            "SELECT currency AS Currency, " +
            "extract_date AS ExtractDate, " +
            "usd_conversion_rate AS UsdConversionRate " +
            "FROM exchange_rates", cancellationToken: cancellationToken);

        var result = await db.QueryAsync<CurrencyExchangeRate>(commandDefinition);
        return result.ToArray();
    }
}
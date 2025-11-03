using System.Data;
using Dapper;

namespace Tests.CurrencyExchangeRates;

public class MonthAnchoredCurrencyExchangeRatesDataService
{
    private readonly Func<IDbConnection> _dbConnectionFactory;

    public MonthAnchoredCurrencyExchangeRatesDataService(Func<IDbConnection> dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<CurrencyExchangeRate?> GetForCurrency(string currency, DateOnly monthAnchor, CancellationToken cancellationToken)
    {
        using var db = _dbConnectionFactory();

        var anchorDateTimeUtc = monthAnchor.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var commandDefinition = new CommandDefinition(commandText:
            """
            SELECT currency        AS Currency,
                   usd_conversion_rate AS UsdConversionRate,
                   extract_date        AS ExtractDate
            FROM   exchange_rates
            WHERE  currency = @currency
              AND  extract_date <= @anchor::date
            ORDER  BY extract_date DESC
            LIMIT 1;
            """,
            parameters: new { currency, anchor = anchorDateTimeUtc },
            cancellationToken: cancellationToken
        );

        var result = await db.QuerySingleOrDefaultAsync<CurrencyExchangeRate>(commandDefinition);

        // For a struct, default instance means no row was found
        return result == default ? null : result;
    }
}
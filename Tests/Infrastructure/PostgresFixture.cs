using System.Data;
using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    public PostgresFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("app")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        await CreateTables();
    }
    
    public string GetConnectionString() => _postgresContainer.GetConnectionString();

    private async ValueTask CreateTables()
    {
        Dapper.Init();
        
        var conn = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await conn.OpenAsync();

        const string createTablesScript = """
                                          CREATE TABLE IF NOT EXISTS latest_extracts (
                                              hotel_id     INTEGER PRIMARY KEY,
                                              extract_date DATE NOT NULL
                                          );

                                          CREATE TABLE IF NOT EXISTS exchange_rates (
                                              currency               TEXT NOT NULL,
                                              extract_date           DATE NOT NULL,
                                              usd_conversion_rate    NUMERIC NOT NULL,
                                              last_update            TIMESTAMP NOT NULL,
                                              PRIMARY KEY (currency, extract_date)
                                          );
                                          """;

        await conn.ExecuteAsync(createTablesScript);
    }

    public async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    public Func<IDbConnection> GetConnectionFactory()
    {
        return () => new NpgsqlConnection(_postgresContainer.GetConnectionString());
    }
}
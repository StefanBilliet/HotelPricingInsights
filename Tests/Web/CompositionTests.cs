using System.Data;
using FakeItEasy;
using Google.Cloud.Bigtable.Admin.V2;
using Google.Cloud.Bigtable.Common.V2;
using Google.Cloud.Bigtable.V2;
using HotelPricingInsights;
using HotelPricingInsights.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Web;

public class CompositionTests
{
    [Fact]
    public void GIVEN_registered_dependencies_WHEN_building_provider_THEN_pricing_controller_is_resolved()
    {
        var services = new ServiceCollection();

        var bigtableClient = A.Fake<BigtableClient>();
        var tableName = TableName.FromProjectInstanceTable("test-project", "test-instance", "pricing");
        var table = new Table { Name = tableName.ToString() };
        services.AddSingleton(bigtableClient);
        services.AddSingleton(table);
        services.AddSingleton<Func<IDbConnection>>(A.Fake<IDbConnection>);

        services.AddLogging();
        services.AddPricingComparisonServices();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        var controller = scope.ServiceProvider.GetRequiredService<PricingComparisonEndpoint>();

        Assert.NotNull(controller);
    }
}

using HotelPricingInsights.Controllers;

namespace Tests.Web;

public class PricingComparisonRequestValidatorTests
{
    private readonly PricingComparisonRequestValidator _validator = new();

    [Fact]
    public void GIVEN_invalid_month_WHEN_validating_THEN_has_error()
    {
        var request = new PricingComparisonRequest
        {
            Month = "2024-13",
            Currency = "USD",
            Hotels = new[] { 1 },
            YearsAgo = 2
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PricingComparisonRequest.Month));
    }

    [Fact]
    public void GIVEN_valid_request_WHEN_validating_THEN_is_valid()
    {
        var request = new PricingComparisonRequest
        {
            Month = "2024-05",
            Currency = "USD",
            Hotels = new[] { 1, 2 },
            YearsAgo = 3,
            Cancellable = true
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}

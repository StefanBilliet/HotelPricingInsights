using AutoFixture.Xunit3;
using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using HotelPricingInsights.Controllers;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Web;

public class PricingComparisonEndpointTests
{
    private readonly IHotelPricingComparisonService _service;
    private readonly PricingComparisonEndpoint _endpoint;
    private readonly IValidator<PricingComparisonRequest> _validator;

    public PricingComparisonEndpointTests()
    {
        _service = A.Fake<IHotelPricingComparisonService>();
        _validator = A.Fake<IValidator<PricingComparisonRequest>>();
        _endpoint = new PricingComparisonEndpoint(_validator, _service);
        A.CallTo(() => _validator.ValidateAsync(A<PricingComparisonRequest>._, A<CancellationToken>._)).Returns(new ValidationResult());
    }

    [Theory, AutoData]
    public async Task GIVEN_invalid_request_WHEN_GetPreCoronaDifference_THEN_returns_bad_request(PricingComparisonRequest invalidRequest)
    {
        A.CallTo(() => _validator.ValidateAsync(invalidRequest, A<CancellationToken>._)).Returns(new ValidationResult { Errors = [new ValidationFailure("Month", "Month is wrong")] });
        
        var result = await _endpoint.GetPreCoronaDifference(invalidRequest, TestContext.Current.CancellationToken);
        
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GIVEN_valid_request_WHEN_GetPreCoronaDifference_THEN_returns_ok_with_response()
    {
        var request = new PricingComparisonRequest
        {
            Month = "2024-05",
            Currency = "USD",
            Hotels = [1, 2],
            YearsAgo = 4,
            Cancellable = false
        };
        var expectedResponse = new PricingComparisonResponse
        {
            Prices =
            [
                new PriceRecord
                {
                    Hotel = 1, ArrivalDate = "2024-05-03", Price = 100m, Currency = "USD", Difference = 10m, 
                }
            ]
        };
        A.CallTo(() => _service.GetPricingComparison(
                A<int[]>.That.Matches(ids => ids.SequenceEqual(new[] { 1, 2 })),
                new DateOnly(2024, 5, 1),
                4,
                "USD",
                A<CancellationToken>._)).Returns(expectedResponse);

        var result = await _endpoint.GetPreCoronaDifference(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
        A.CallTo(() => _service.GetPricingComparison(
                A<int[]>.That.Matches(ids => ids.SequenceEqual(new[] { 1, 2 })),
                new DateOnly(2024, 5, 1),
                4,
                "USD",
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
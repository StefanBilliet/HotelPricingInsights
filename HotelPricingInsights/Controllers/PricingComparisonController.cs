using System.Globalization;
using FluentValidation;
using HotelPricingInsights.Controllers.HotelPriceComparison;
using Microsoft.AspNetCore.Mvc;

namespace HotelPricingInsights.Controllers;

[ApiController]
[Route("pricing/pre_corona_difference")]
public class PricingComparisonController : ControllerBase
{
    private readonly IValidator<PricingComparisonRequest> _validator;
    private readonly IHotelPricingComparisonService _service;

    public PricingComparisonController(IValidator<PricingComparisonRequest> validator,
        IHotelPricingComparisonService service)
    {
        _validator = validator;
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PricingComparisonResponse>> GetPreCoronaDifference(
        [FromQuery] PricingComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid) 
        {
            result.AddToModelState(ModelState);
            return BadRequest(ModelState);
        }
        
        var arrivalMonth = DateOnly.ParseExact($"{request.Month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var comparison = await _service.GetPricingComparison(
            request.Hotels,
            arrivalMonth,
            request.YearsAgo,
            request.Currency,
            cancellationToken);

        return Ok(comparison);
    }

}

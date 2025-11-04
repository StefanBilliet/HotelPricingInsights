using System.Globalization;
using FluentValidation;

namespace HotelPricingInsights.Controllers;

public class PricingComparisonRequestValidator : AbstractValidator<PricingComparisonRequest>
{
    public PricingComparisonRequestValidator()
    {
        RuleFor(r => r.Month)
            .NotEmpty().WithMessage("month is required.")
            .Matches("^\\d{4}-\\d{2}$").WithMessage("month must be in format YYYY-MM.")
            .Must(BeValidMonth).WithMessage("month must represent a valid calendar month.");

        RuleFor(r => r.Currency)
            .NotEmpty().WithMessage("currency is required.")
            .Matches("^[A-Z]{3}$").WithMessage("currency must be a 3-letter uppercase ISO code.");

        RuleFor(r => r.Hotels)
            .NotNull().WithMessage("hotels are required.")
            .Must(h => h.Length is >= 1 and <= 10).WithMessage("hotels must contain between 1 and 10 items.");

        RuleForEach(r => r.Hotels)
            .GreaterThan(0).WithMessage("hotel identifiers must be positive.");

        RuleFor(r => r.YearsAgo)
            .InclusiveBetween(1, 5).WithMessage("years_ago must be between 1 and 5.");
    }

    private static bool BeValidMonth(string month) =>
        DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}

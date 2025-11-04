using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HotelPricingInsights.Controllers;

public static class ExtensionsForModelState
{
    public static void AddToModelState(this ValidationResult validationResult, ModelStateDictionary modelState)
    {
        foreach (var error in validationResult.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
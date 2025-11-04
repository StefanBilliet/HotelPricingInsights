using FluentValidation;
using HotelPricingInsights.Controllers;
using HotelPricingInsights.Controllers.HotelPriceComparison;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IHotelPricingComparisonService, HotelPricingComparisonService>();
builder.Services.AddTransient<IValidator<PricingComparisonRequest>, PricingComparisonRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

namespace HotelPricingInsights
{
    public partial class Program;
}

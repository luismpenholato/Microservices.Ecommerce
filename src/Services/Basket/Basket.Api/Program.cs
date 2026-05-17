using Basket.Application;
using Basket.Infrastructure;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("BasketService");

builder.Services.AddBuildingBlocksWeb();
builder.Services.AddEcommerceJwtAuthenticationWithoutFallbackPolicy(builder.Configuration);
builder.Services.AddBasketApplication();
builder.Services.AddBasketInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Basket.Application.Baskets.Commands.AddBasketItemCommand>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseBuildingBlocksWeb();
app.UseEcommerceAuthentication();
app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

public partial class Program;

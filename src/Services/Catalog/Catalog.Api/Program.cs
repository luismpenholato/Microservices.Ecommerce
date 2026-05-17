using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("CatalogService");

builder.Services.AddBuildingBlocksWeb();
builder.Services.AddEcommerceJwtAuthenticationWithoutFallbackPolicy(builder.Configuration);
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Catalog.Application.Products.Commands.CreateProductCommand>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

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
await CatalogDataSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program;

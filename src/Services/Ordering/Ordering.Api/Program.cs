using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using FluentValidation;
using FluentValidation.AspNetCore;
using Ordering.Application;
using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("OrderingService");

builder.Services.AddBuildingBlocksWeb();
builder.Services.AddEcommerceJwtAuthenticationWithoutFallbackPolicy(builder.Configuration);
builder.Services.AddOrderingApplication();
builder.Services.AddOrderingInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Ordering.Application.Orders.Commands.CreateOrderCommand>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseBuildingBlocksWeb();
app.UseEcommerceAuthentication();
app.UseMiddleware<Ordering.Api.Middleware.IdempotencyConflictMiddleware>();
app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
await Ordering.Infrastructure.DependencyInjection.MigrateOrderingDatabaseAsync(app.Services);
app.Run();

public partial class Program;

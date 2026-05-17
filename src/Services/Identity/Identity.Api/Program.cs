using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using FluentValidation;
using FluentValidation.AspNetCore;
using Identity.Application;
using Identity.Application.Auth.Commands;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("IdentityService");

builder.Services.AddBuildingBlocksWeb();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddEcommerceJwtAuthenticationWithoutFallbackPolicy(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommand>();
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
await IdentityDataSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program;

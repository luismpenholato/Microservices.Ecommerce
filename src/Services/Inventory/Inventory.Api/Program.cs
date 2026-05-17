using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("InventoryService");

builder.Services.AddBuildingBlocksWeb();
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseBuildingBlocksWeb();
app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
await InventoryDataSeeder.SeedAsync(app.Services);
app.Run();

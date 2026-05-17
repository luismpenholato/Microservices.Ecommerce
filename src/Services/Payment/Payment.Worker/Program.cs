using BuildingBlocks.Observability;
using Payment.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://+:8080");

var paymentDb = builder.Configuration.GetConnectionString("PaymentDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentDb' not configured.");

builder.AddWorkerOperationalHost("Payment.Worker", health =>
{
    health.AddNpgSql(paymentDb, name: "payment-db", tags: ["ready"]);
    health.AddRabbitMqHealthCheck(builder.Configuration);
});

builder.Services.AddPaymentInfrastructure(builder.Configuration);

var app = builder.Build();
await DependencyInjection.MigratePaymentDatabaseAsync(app.Services);
app.MapWorkerOperationalEndpoints();
await app.RunAsync();

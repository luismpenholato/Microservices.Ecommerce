using BuildingBlocks.Observability;
using Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://+:8080");

var notificationDb = builder.Configuration.GetConnectionString("NotificationDb")
    ?? throw new InvalidOperationException("Connection string 'NotificationDb' not configured.");

builder.AddWorkerOperationalHost("Notification.Worker", health =>
{
    health.AddNpgSql(notificationDb, name: "notification-db", tags: ["ready"]);
    health.AddRabbitMqHealthCheck(builder.Configuration);
});

builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();
await DependencyInjection.MigrateNotificationDatabaseAsync(app.Services);
app.MapWorkerOperationalEndpoints();
await app.RunAsync();

using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Orders;
using Notification.Infrastructure.Messaging.Consumers;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException("Connection string 'NotificationDb' not configured.");

        services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIntegrationEventUnitOfWork, IntegrationEventUnitOfWork>();
        services.AddScoped<OrderCompletedHandler>();
        services.AddScoped<OrderCancelledHandler>();

        services.AddMessageBus(configuration, "NotificationService", x =>
        {
            x.AddConsumer<OrderCompletedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();
        });

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "notification-db", tags: ["ready"])
            .AddRabbitMqHealthCheck(configuration);

        return services;
    }

    public static async Task MigrateNotificationDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

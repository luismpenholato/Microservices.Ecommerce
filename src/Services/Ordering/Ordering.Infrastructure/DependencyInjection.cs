using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Messaging.Consumers;
using Ordering.Infrastructure.Outbox;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderingDb")
            ?? throw new InvalidOperationException("Connection string 'OrderingDb' not configured.");

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIntegrationEventUnitOfWork, IntegrationEventUnitOfWork>();
        services.AddSingleton<IOrderMetrics, OrderMetrics>();
        services.AddSingleton<IOutboxPendingCountProvider, OutboxPendingCountProvider>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddMessageBus(configuration, "OrderingService", x =>
        {
            x.AddConsumer<PaymentApprovedConsumer>();
            x.AddConsumer<PaymentRejectedConsumer>();
            x.AddConsumer<StockReservedConsumer>();
            x.AddConsumer<StockReservationFailedConsumer>();
        });

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "ordering-db", tags: ["ready"])
            .AddRabbitMqHealthCheck(configuration);

        return services;
    }

    public static async Task MigrateOrderingDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Metrics;
using Inventory.Application.Abstractions;
using Inventory.Application.Payments;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Outbox;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Connection string 'InventoryDb' not configured.");

        services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIntegrationEventUnitOfWork, IntegrationEventUnitOfWork>();
        services.AddScoped<PaymentApprovedHandler>();
        services.AddSingleton<IStockReservationMetrics, StockReservationMetrics>();
        services.AddSingleton<IOutboxPendingCountProvider, OutboxPendingCountProvider>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddMessageBus(configuration, "InventoryService", x =>
            x.AddConsumer<PaymentApprovedConsumer>());

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "inventory-db", tags: ["ready"])
            .AddRabbitMqHealthCheck(configuration);

        return services;
    }
}

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using BuildingBlocks.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Application.Orders;
using Payment.Infrastructure.Messaging.Consumers;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentDb")
            ?? throw new InvalidOperationException("Connection string 'PaymentDb' not configured.");

        services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIntegrationEventUnitOfWork, IntegrationEventUnitOfWork>();
        services.AddScoped<OrderCreatedHandler>();
        services.AddSingleton<IPaymentDecisionService, PaymentDecisionService>();
        services.AddSingleton<IOutboxPendingCountProvider, OutboxPendingCountProvider>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddMessageBus(configuration, "PaymentService", x => x.AddConsumer<OrderCreatedConsumer>());

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "payment-db", tags: ["ready"])
            .AddRabbitMqHealthCheck(configuration);

        return services;
    }

    public static async Task MigratePaymentDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

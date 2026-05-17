using System.Diagnostics.Metrics;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Observability.Metrics;

public static class OutboxPendingMetricsRegistration
{
    public static IServiceCollection AddOutboxPendingGauge(this IServiceCollection services)
    {
        services.AddSingleton<OutboxPendingGaugeHolder>();
        return services;
    }

    internal sealed class OutboxPendingGaugeHolder
    {
        public OutboxPendingGaugeHolder(
            IServiceProvider serviceProvider,
            IOptions<ServiceInfoOptions> serviceOptions)
        {
            var serviceName = serviceOptions.Value.Name;

            EcommerceMeters.Meter.CreateObservableGauge(
                "ecommerce_outbox_messages_pending",
                () => Observe(serviceProvider, serviceName),
                description: "Outbox messages waiting to be published");
        }

        private static IEnumerable<Measurement<long>> Observe(IServiceProvider serviceProvider, string serviceName)
        {
            using var scope = serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetService<IOutboxPendingCountProvider>()
                ?? new NoOpOutboxPendingCountProvider();
            var count = provider.GetPendingCount();

            return
            [
                new Measurement<long>(count, new KeyValuePair<string, object?>("service", serviceName))
            ];
        }
    }
}

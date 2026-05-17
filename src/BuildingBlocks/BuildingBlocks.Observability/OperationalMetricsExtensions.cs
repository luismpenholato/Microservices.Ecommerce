using BuildingBlocks.Messaging.Metrics;
using BuildingBlocks.Observability.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Observability;

public static class OperationalMetricsExtensions
{
    public static IServiceCollection AddEcommerceOperationalMetrics(
        this IServiceCollection services,
        string serviceName)
    {
        services.Configure<ServiceInfoOptions>(options => options.Name = serviceName);

        services.AddSingleton<IConsumerMetricsRecorder, ConsumerMetricsRecorder>();
        services.AddSingleton<IOutboxMetricsRecorder, OutboxMetricsRecorder>();
        services.AddOutboxPendingGauge();

        return services;
    }
}

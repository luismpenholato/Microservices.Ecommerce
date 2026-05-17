using System.Diagnostics.Metrics;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Observability.Metrics;

public sealed class ConsumerMetricsRecorder(IOptions<ServiceInfoOptions> serviceOptions) : IConsumerMetricsRecorder
{
    private readonly string _service = serviceOptions.Value.Name;

    private static readonly Counter<long> ProcessedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_consumer_messages_processed_total",
        description: "Integration events committed by consumer");

    private static readonly Counter<long> FailedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_consumer_messages_failed_total",
        description: "Integration event consumer faults");

    public void RecordProcessed(string consumerName) =>
        ProcessedCounter.Add(1,
            new KeyValuePair<string, object?>("service", _service),
            new KeyValuePair<string, object?>("consumer_name", consumerName));

    public void RecordFailed(string consumerName) =>
        FailedCounter.Add(1,
            new KeyValuePair<string, object?>("service", _service),
            new KeyValuePair<string, object?>("consumer_name", consumerName));
}

using System.Diagnostics.Metrics;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Observability.Metrics;

public sealed class OutboxMetricsRecorder(IOptions<ServiceInfoOptions> serviceOptions) : IOutboxMetricsRecorder
{
    private readonly string _service = serviceOptions.Value.Name;

    private static readonly Counter<long> PublishedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_outbox_messages_published_total",
        description: "Outbox messages published successfully");

    private static readonly Counter<long> PublishFailuresCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_outbox_messages_publish_failures_total",
        description: "Outbox publish attempts that failed");

    private static readonly Counter<long> ExhaustedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_outbox_messages_exhausted_total",
        description: "Outbox messages that exhausted publish retries");

    public void RecordPublished() =>
        PublishedCounter.Add(1, new KeyValuePair<string, object?>("service", _service));

    public void RecordPublishFailure() =>
        PublishFailuresCounter.Add(1, new KeyValuePair<string, object?>("service", _service));

    public void RecordExhausted() =>
        ExhaustedCounter.Add(1, new KeyValuePair<string, object?>("service", _service));
}

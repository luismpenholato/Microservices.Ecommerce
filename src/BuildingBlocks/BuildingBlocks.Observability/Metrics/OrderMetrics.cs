using System.Diagnostics.Metrics;
using Ordering.Application.Abstractions;

namespace BuildingBlocks.Observability.Metrics;

public sealed class OrderMetrics : IOrderMetrics
{
    private static readonly Counter<long> CreatedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_orders_created_total",
        description: "Orders created");

    private static readonly Counter<long> CompletedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_orders_completed_total",
        description: "Orders completed");

    private static readonly Counter<long> CancelledCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_orders_cancelled_total",
        description: "Orders cancelled");

    private static readonly Counter<long> FailedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_orders_failed_total",
        description: "Orders failed");

    public void RecordOrderCreated() => CreatedCounter.Add(1);

    public void RecordOrderCompleted() => CompletedCounter.Add(1);

    public void RecordOrderCancelled() => CancelledCounter.Add(1);

    public void RecordOrderFailed() => FailedCounter.Add(1);
}

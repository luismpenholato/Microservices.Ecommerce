using System.Diagnostics.Metrics;
using Inventory.Application.Abstractions;

namespace BuildingBlocks.Observability.Metrics;

public sealed class StockReservationMetrics : IStockReservationMetrics
{
    private static readonly Counter<long> ApprovedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_stock_reservations_approved_total",
        description: "Stock reservations approved");

    private static readonly Counter<long> FailedCounter = EcommerceMeters.Meter.CreateCounter<long>(
        "ecommerce_stock_reservations_failed_total",
        description: "Stock reservations failed");

    public void RecordReservationApproved() => ApprovedCounter.Add(1);

    public void RecordReservationFailed() => FailedCounter.Add(1);
}

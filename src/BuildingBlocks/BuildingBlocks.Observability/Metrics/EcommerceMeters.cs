using System.Diagnostics.Metrics;

namespace BuildingBlocks.Observability.Metrics;

public static class EcommerceMeters
{
    public const string MeterName = "Microservices.Ecommerce";

    public static readonly Meter Meter = new(MeterName);
}

using BuildingBlocks.Observability.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Observability;

public static class HealthChecksExtensions
{
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration) =>
        builder.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

    public static IHealthChecksBuilder AddLiveCheck(this IHealthChecksBuilder builder) =>
        builder.AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
            tags: ["live"]);
}

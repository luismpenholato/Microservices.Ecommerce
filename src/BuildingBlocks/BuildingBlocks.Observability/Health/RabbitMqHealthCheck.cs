using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace BuildingBlocks.Observability.Health;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var user = configuration["RabbitMq:Username"] ?? "guest";
        var pass = configuration["RabbitMq:Password"] ?? "guest";

        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri($"amqp://{user}:{pass}@{host}:5672/")
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ connection established.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed.", ex);
        }
    }
}

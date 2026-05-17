using BuildingBlocks.Contracts;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public static class IntegrationEventLogScope
{
    public static IDisposable Begin(ILogger logger, IntegrationEvent integrationEvent, string? consumerName = null)
    {
        var orderId = IntegrationEventLogging.TryGetOrderId(integrationEvent);
        var state = new Dictionary<string, object?>
        {
            ["EventId"] = integrationEvent.EventId,
            ["CorrelationId"] = integrationEvent.CorrelationId,
            ["MessageType"] = integrationEvent.GetType().Name
        };

        if (orderId is not null)
        {
            state["OrderId"] = orderId.Value;
        }

        if (!string.IsNullOrWhiteSpace(consumerName))
        {
            state["ConsumerName"] = consumerName;
        }

        return logger.BeginScope(state)!;
    }
}

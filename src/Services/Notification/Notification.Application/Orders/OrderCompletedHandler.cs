using BuildingBlocks.Contracts;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Orders;

public sealed class OrderCompletedHandler(ILogger<OrderCompletedHandler> logger)
{
    public Task HandleAsync(OrderCompletedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification sent (simulated) for completed order {OrderId}. EventId={EventId} CorrelationId={CorrelationId}",
            message.OrderId,
            message.EventId,
            message.CorrelationId);
        return Task.CompletedTask;
    }
}

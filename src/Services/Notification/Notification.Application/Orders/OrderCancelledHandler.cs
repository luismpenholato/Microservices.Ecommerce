using BuildingBlocks.Contracts;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Orders;

public sealed class OrderCancelledHandler(ILogger<OrderCancelledHandler> logger)
{
    public Task HandleAsync(OrderCancelledEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification sent (simulated) for cancelled order {OrderId}. Reason context in event. EventId={EventId} CorrelationId={CorrelationId}",
            message.OrderId,
            message.EventId,
            message.CorrelationId);
        return Task.CompletedTask;
    }
}

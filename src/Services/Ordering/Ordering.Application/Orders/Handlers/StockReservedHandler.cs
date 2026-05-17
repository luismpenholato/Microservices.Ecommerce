using BuildingBlocks.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Domain.Enums;
using Ordering.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ordering.Application.Orders.Handlers;

public sealed class StockReservedHandler(
    IOrderRepository repository,
    IOutboxWriter outboxWriter,
    IOrderMetrics orderMetrics,
    ILogger<StockReservedHandler> logger)
{
    public async Task HandleAsync(StockReservedEvent message, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(message.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(message.OrderId);

        var alreadyCompleted = order.Status == OrderStatus.Completed;
        order.MarkStockReserved();
        order.MarkCompleted();
        await repository.UpdateAsync(order, cancellationToken);

        if (!alreadyCompleted)
        {
            orderMetrics.RecordOrderCompleted();
            outboxWriter.Enqueue(new OrderCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = order.Id,
                CustomerId = order.CustomerId
            });
        }

        logger.LogInformation(
            "Order {OrderId} completed. EventId={EventId} CorrelationId={CorrelationId}",
            order.Id,
            message.EventId,
            message.CorrelationId);
    }
}

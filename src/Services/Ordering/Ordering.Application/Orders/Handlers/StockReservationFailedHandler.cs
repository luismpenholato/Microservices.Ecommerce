using BuildingBlocks.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ordering.Application.Orders.Handlers;

public sealed class StockReservationFailedHandler(
    IOrderRepository repository,
    IOrderMetrics orderMetrics,
    ILogger<StockReservationFailedHandler> logger)
{
    public async Task HandleAsync(StockReservationFailedEvent message, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(message.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(message.OrderId);

        order.MarkFailed();
        await repository.UpdateAsync(order, cancellationToken);

        orderMetrics.RecordOrderFailed();

        logger.LogWarning(
            "Order {OrderId} failed. Reason={Reason} EventId={EventId} CorrelationId={CorrelationId}",
            order.Id,
            message.Reason,
            message.EventId,
            message.CorrelationId);
    }
}

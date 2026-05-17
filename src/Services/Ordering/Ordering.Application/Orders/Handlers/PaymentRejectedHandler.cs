using BuildingBlocks.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ordering.Application.Orders.Handlers;

public sealed class PaymentRejectedHandler(
    IOrderRepository repository,
    IOrderMetrics orderMetrics,
    ILogger<PaymentRejectedHandler> logger)
{
    public async Task HandleAsync(PaymentRejectedEvent message, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(message.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(message.OrderId);

        order.MarkPaymentRejected();
        await repository.UpdateAsync(order, cancellationToken);

        orderMetrics.RecordOrderCancelled();

        logger.LogWarning(
            "Order {OrderId} marked PaymentRejected. EventId={EventId} CorrelationId={CorrelationId}",
            order.Id,
            message.EventId,
            message.CorrelationId);
    }
}

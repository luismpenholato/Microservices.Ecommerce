using BuildingBlocks.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ordering.Application.Orders.Handlers;

public sealed class PaymentApprovedHandler(
    IOrderRepository repository,
    ILogger<PaymentApprovedHandler> logger)
{
    public async Task HandleAsync(PaymentApprovedEvent message, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(message.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(message.OrderId);

        order.MarkPaymentApproved();
        await repository.UpdateAsync(order, cancellationToken);

        logger.LogInformation(
            "Order {OrderId} marked PaymentApproved. EventId={EventId} CorrelationId={CorrelationId}",
            order.Id,
            message.EventId,
            message.CorrelationId);
    }
}

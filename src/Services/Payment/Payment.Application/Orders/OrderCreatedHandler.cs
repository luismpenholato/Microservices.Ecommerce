using BuildingBlocks.Contracts;
using Payment.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Orders;

public sealed class OrderCreatedHandler(
    IPaymentDecisionService decisionService,
    IOutboxWriter outboxWriter,
    ILogger<OrderCreatedHandler> logger)
{
    public Task HandleAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        if (decisionService.ShouldApprove(message.OrderId))
        {
            outboxWriter.Enqueue(new PaymentApprovedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                CustomerId = message.CustomerId,
                Amount = message.TotalAmount,
                TransactionId = $"TX-{Guid.NewGuid():N}",
                Items = message.Items
            });

            logger.LogInformation(
                "Payment approved for order {OrderId}. EventId={EventId} CorrelationId={CorrelationId}",
                message.OrderId,
                message.EventId,
                message.CorrelationId);
        }
        else
        {
            outboxWriter.Enqueue(new PaymentRejectedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                CustomerId = message.CustomerId,
                Reason = "Simulated payment rejection"
            });

            logger.LogWarning(
                "Payment rejected for order {OrderId}. EventId={EventId} CorrelationId={CorrelationId}",
                message.OrderId,
                message.EventId,
                message.CorrelationId);
        }

        return Task.CompletedTask;
    }
}

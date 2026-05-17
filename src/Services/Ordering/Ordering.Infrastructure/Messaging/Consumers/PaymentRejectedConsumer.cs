using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Handlers;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class PaymentRejectedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    PaymentRejectedHandler handler,
    ILogger<PaymentRejectedConsumer> logger)
    : TransactionalIdempotentConsumer<PaymentRejectedEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<PaymentRejectedEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

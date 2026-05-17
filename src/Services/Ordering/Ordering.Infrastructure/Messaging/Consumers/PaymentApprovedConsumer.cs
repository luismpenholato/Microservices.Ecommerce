using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Handlers;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class PaymentApprovedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    PaymentApprovedHandler handler,
    ILogger<PaymentApprovedConsumer> logger)
    : TransactionalIdempotentConsumer<PaymentApprovedEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<PaymentApprovedEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

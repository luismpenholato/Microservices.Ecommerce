using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Application.Orders;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class OrderCompletedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    OrderCompletedHandler handler,
    ILogger<OrderCompletedConsumer> logger)
    : TransactionalIdempotentConsumer<OrderCompletedEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<OrderCompletedEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

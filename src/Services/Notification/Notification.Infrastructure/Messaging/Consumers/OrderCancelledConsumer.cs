using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Application.Orders;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class OrderCancelledConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    OrderCancelledHandler handler,
    ILogger<OrderCancelledConsumer> logger)
    : TransactionalIdempotentConsumer<OrderCancelledEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<OrderCancelledEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

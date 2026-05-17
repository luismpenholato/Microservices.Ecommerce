using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Handlers;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class StockReservationFailedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    StockReservationFailedHandler handler,
    ILogger<StockReservationFailedConsumer> logger)
    : TransactionalIdempotentConsumer<StockReservationFailedEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<StockReservationFailedEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

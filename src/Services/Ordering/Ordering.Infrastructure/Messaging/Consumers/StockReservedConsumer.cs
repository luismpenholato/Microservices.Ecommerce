using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Handlers;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class StockReservedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    StockReservedHandler handler,
    ILogger<StockReservedConsumer> logger)
    : TransactionalIdempotentConsumer<StockReservedEvent>(unitOfWork, logger)
{
    protected override Task HandleAsync(ConsumeContext<StockReservedEvent> context, CancellationToken cancellationToken) =>
        handler.HandleAsync(context.Message, cancellationToken);
}

using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Application.Orders;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Messaging.Consumers;

public sealed class OrderCreatedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    OrderCreatedHandler handler,
    PaymentDbContext dbContext,
    ILogger<OrderCreatedConsumer> logger)
    : TransactionalIdempotentConsumer<OrderCreatedEvent>(unitOfWork, logger)
{
    protected override async Task HandleAsync(ConsumeContext<OrderCreatedEvent> context, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(context.Message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Inventory.Application.Payments;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class PaymentApprovedConsumer(
    IIntegrationEventUnitOfWork unitOfWork,
    PaymentApprovedHandler handler,
    InventoryDbContext dbContext,
    ILogger<PaymentApprovedConsumer> logger)
    : TransactionalIdempotentConsumer<PaymentApprovedEvent>(unitOfWork, logger)
{
    protected override async Task HandleAsync(ConsumeContext<PaymentApprovedEvent> context, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(context.Message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

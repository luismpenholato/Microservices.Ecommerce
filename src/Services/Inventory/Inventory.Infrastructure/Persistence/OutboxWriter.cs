using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Persistence.Entities;

namespace Inventory.Infrastructure.Persistence;

public sealed class OutboxWriter(InventoryDbContext dbContext) : IOutboxWriter
{
    public void Enqueue(IntegrationEvent integrationEvent)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = integrationEvent.EventId,
            EventType = integrationEvent.GetType().Name,
            Payload = IntegrationEventSerializer.Serialize(integrationEvent),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}

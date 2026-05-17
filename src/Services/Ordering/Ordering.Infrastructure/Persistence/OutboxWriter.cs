using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Persistence.Entities;

namespace Ordering.Infrastructure.Persistence;

public sealed class OutboxWriter(OrderingDbContext dbContext) : IOutboxWriter
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
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });
    }
}

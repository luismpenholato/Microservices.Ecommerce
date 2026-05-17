using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Payment.Application.Abstractions;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class OutboxWriter(PaymentDbContext dbContext) : IOutboxWriter
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

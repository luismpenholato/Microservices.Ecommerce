using BuildingBlocks.Contracts;

namespace BuildingBlocks.Messaging;

public interface IIntegrationEventUnitOfWork
{
    Task ExecuteIdempotentAsync<TEvent>(
        TEvent message,
        string consumerName,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}

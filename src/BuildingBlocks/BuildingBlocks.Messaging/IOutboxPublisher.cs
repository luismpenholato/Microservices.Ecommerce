namespace BuildingBlocks.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(object integrationEvent, CancellationToken cancellationToken);
}

namespace BuildingBlocks.Messaging;

public interface IProcessedEventStore
{
    Task<bool> HasBeenProcessedAsync(
        Guid eventId,
        string consumerName,
        CancellationToken cancellationToken);

    Task MarkAsProcessedAsync(
        Guid eventId,
        string consumerName,
        string eventType,
        CancellationToken cancellationToken);
}

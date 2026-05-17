namespace BuildingBlocks.Messaging.Metrics;

public interface IOutboxPendingCountProvider
{
    long GetPendingCount();
}

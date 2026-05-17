namespace BuildingBlocks.Messaging.Metrics;

public sealed class NoOpOutboxPendingCountProvider : IOutboxPendingCountProvider
{
    public long GetPendingCount() => 0;
}

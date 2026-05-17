using System.Collections.Concurrent;
using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;

namespace IntegrationTests.Infrastructure;

public sealed class TestConsumerExecutionFaultHook : IConsumerExecutionFaultHook
{
    private readonly ConcurrentDictionary<Guid, int> _attemptsByEventId = new();

    public string? TargetConsumerName { get; set; }

    public void OnBeforeHandle(string consumerName, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (TargetConsumerName is not null &&
            !string.Equals(consumerName, TargetConsumerName, StringComparison.Ordinal))
        {
            return;
        }

        var attempt = _attemptsByEventId.AddOrUpdate(integrationEvent.EventId, 1, (_, count) => count + 1);
        if (attempt == 1)
        {
            throw new InvalidOperationException(
                $"Simulated transient failure for {consumerName} on attempt {attempt}. EventId={integrationEvent.EventId}");
        }
    }

    public int GetAttemptCount(Guid eventId) =>
        _attemptsByEventId.TryGetValue(eventId, out var count) ? count : 0;
}

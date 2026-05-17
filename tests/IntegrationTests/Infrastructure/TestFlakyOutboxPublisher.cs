using BuildingBlocks.Messaging;

namespace IntegrationTests.Infrastructure;

public sealed class TestFlakyOutboxPublisher(IOutboxPublisher innerPublisher) : IOutboxPublisher
{
    private int _failuresBeforeSuccess = 2;
    private int _attempts;

    public int FailuresBeforeSuccess
    {
        get => _failuresBeforeSuccess;
        set
        {
            _failuresBeforeSuccess = value;
            _attempts = 0;
        }
    }

    public int AttemptCount => _attempts;

    public Task PublishAsync(object integrationEvent, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        if (attempt <= _failuresBeforeSuccess)
        {
            throw new IOException(
                $"Simulated broker unavailable (attempt {attempt}/{_failuresBeforeSuccess}).");
        }

        return innerPublisher.PublishAsync(integrationEvent, cancellationToken);
    }
}

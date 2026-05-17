using BuildingBlocks.Contracts;

namespace BuildingBlocks.Messaging;

public sealed class NoOpConsumerExecutionFaultHook : IConsumerExecutionFaultHook
{
    public void OnBeforeHandle(string consumerName, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
    }
}

using BuildingBlocks.Contracts;

namespace BuildingBlocks.Messaging;

public interface IConsumerExecutionFaultHook
{
    void OnBeforeHandle(string consumerName, IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

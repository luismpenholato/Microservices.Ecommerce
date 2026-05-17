using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public sealed class IntegrationEventUnitOfWorkExecutor(
    IConsumerExecutionFaultHook faultHook,
    IConsumerMetricsRecorder metrics,
    ILogger<IntegrationEventUnitOfWorkExecutor> logger)
{
    public async Task ExecuteIdempotentAsync<TEvent>(
        TEvent integrationEvent,
        string consumerName,
        Func<Guid, string, CancellationToken, Task<bool>> isAlreadyProcessed,
        Func<Func<CancellationToken, Task>, CancellationToken, Task> executeInTransaction,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        using (IntegrationEventLogScope.Begin(logger, integrationEvent, consumerName))
        {
            if (await isAlreadyProcessed(integrationEvent.EventId, consumerName, cancellationToken))
            {
                logger.LogInformation(
                    "Integration event already processed. ConsumerName={ConsumerName} EventId={EventId} CorrelationId={CorrelationId}",
                    consumerName,
                    integrationEvent.EventId,
                    integrationEvent.CorrelationId);
                return;
            }

            await executeInTransaction(async ct =>
            {
                faultHook.OnBeforeHandle(consumerName, integrationEvent, ct);
                await handler(ct);
            }, cancellationToken);

            metrics.RecordProcessed(consumerName);
        }
    }
}

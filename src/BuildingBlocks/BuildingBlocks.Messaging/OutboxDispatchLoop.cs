using BuildingBlocks.Messaging.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging;

public static class OutboxDispatchLoop
{
    public sealed class OutboxMessageSnapshot
    {
        public required Guid Id { get; init; }
        public required Guid EventId { get; init; }
        public required string EventType { get; init; }
        public required string Payload { get; init; }
        public required Guid CorrelationId { get; init; }
        public DateTime? ProcessedAtUtc { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }

    public static async Task<int> DispatchBatchAsync(
        IReadOnlyList<OutboxMessageSnapshot> messages,
        IOutboxPublisher publisher,
        IOutboxMetricsRecorder metrics,
        IOptions<OutboxOptions> options,
        ILogger logger,
        string serviceLabel,
        Func<OutboxMessageSnapshot, CancellationToken, Task> saveMessageAsync,
        CancellationToken cancellationToken)
    {
        var maxRetries = options.Value.MaxPublishRetries;
        var dispatched = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (logger.BeginScope(new Dictionary<string, object?>
            {
                ["OutboxId"] = message.Id,
                ["EventId"] = message.EventId,
                ["CorrelationId"] = message.CorrelationId,
                ["EventType"] = message.EventType
            }))
            {
                try
                {
                    var integrationEvent = IntegrationEventSerializer.Deserialize(message.Payload, message.EventType);
                    await publisher.PublishAsync(integrationEvent, cancellationToken);

                    message.ProcessedAtUtc = DateTime.UtcNow;
                    message.LastError = null;
                    metrics.RecordPublished();
                    dispatched++;

                    logger.LogInformation(
                        "{Service} outbox message published. OutboxId={OutboxId} EventId={EventId} EventType={EventType} CorrelationId={CorrelationId}",
                        serviceLabel,
                        message.Id,
                        message.EventId,
                        message.EventType,
                        message.CorrelationId);

                    await saveMessageAsync(message, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(
                        "{Service} outbox publish cancelled. OutboxId={OutboxId} EventId={EventId}",
                        serviceLabel,
                        message.Id,
                        message.EventId);
                    throw;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.LastError = ex.Message;
                    metrics.RecordPublishFailure();

                    if (message.RetryCount >= maxRetries)
                    {
                        metrics.RecordExhausted();
                        logger.LogError(
                            ex,
                            "{Service} outbox publish exhausted retries. OutboxId={OutboxId} EventId={EventId} RetryCount={RetryCount} MaxPublishRetries={MaxPublishRetries}",
                            serviceLabel,
                            message.Id,
                            message.EventId,
                            message.RetryCount,
                            maxRetries);
                    }
                    else
                    {
                        logger.LogWarning(
                            ex,
                            "{Service} outbox publish failed (will retry). OutboxId={OutboxId} EventId={EventId} RetryCount={RetryCount} MaxPublishRetries={MaxPublishRetries}",
                            serviceLabel,
                            message.Id,
                            message.EventId,
                            message.RetryCount,
                            maxRetries);
                    }

                    await saveMessageAsync(message, cancellationToken);
                }
            }
        }

        return dispatched;
    }
}

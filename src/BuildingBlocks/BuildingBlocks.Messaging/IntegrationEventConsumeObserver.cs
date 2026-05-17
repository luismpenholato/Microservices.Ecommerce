using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging.Metrics;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public sealed class IntegrationEventConsumeObserver(
    IConsumerMetricsRecorder metrics,
    ILogger<IntegrationEventConsumeObserver> logger) : IConsumeObserver
{
    public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        var messageType = typeof(T).Name;
        var consumerName = context.ReceiveContext.InputAddress?.Segments.LastOrDefault() ?? "unknown";

        metrics.RecordFailed(consumerName);

        if (context.Message is IntegrationEvent integrationEvent)
        {
            var orderId = IntegrationEventLogging.TryGetOrderId(integrationEvent);

            logger.LogError(
                exception,
                "Consume fault. MessageType={MessageType} ConsumerName={ConsumerName} EventId={EventId} CorrelationId={CorrelationId} OrderId={OrderId}",
                messageType,
                consumerName,
                integrationEvent.EventId,
                integrationEvent.CorrelationId,
                orderId);
        }
        else
        {
            logger.LogError(
                exception,
                "Consume fault. MessageType={MessageType} ConsumerName={ConsumerName}",
                messageType,
                consumerName);
        }

        return Task.CompletedTask;
    }
}

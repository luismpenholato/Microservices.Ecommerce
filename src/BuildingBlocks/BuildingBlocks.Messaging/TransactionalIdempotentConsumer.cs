using BuildingBlocks.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public abstract class TransactionalIdempotentConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly IIntegrationEventUnitOfWork _unitOfWork;
    private readonly ILogger _logger;
    private readonly string _consumerName;

    protected TransactionalIdempotentConsumer(
        IIntegrationEventUnitOfWork unitOfWork,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _consumerName = GetType().Name;
    }

    public Task Consume(ConsumeContext<TEvent> context)
    {
        using (IntegrationEventLogScope.Begin(_logger, context.Message, _consumerName))
        {
            _logger.LogDebug(
                "Consuming integration event. ConsumerName={ConsumerName} MessageType={MessageType} EventId={EventId} CorrelationId={CorrelationId}",
                _consumerName,
                typeof(TEvent).Name,
                context.Message.EventId,
                context.Message.CorrelationId);

            return _unitOfWork.ExecuteIdempotentAsync(
                context.Message,
                _consumerName,
                ct => HandleAsync(context, ct),
                context.CancellationToken);
        }
    }

    protected abstract Task HandleAsync(ConsumeContext<TEvent> context, CancellationToken cancellationToken);
}

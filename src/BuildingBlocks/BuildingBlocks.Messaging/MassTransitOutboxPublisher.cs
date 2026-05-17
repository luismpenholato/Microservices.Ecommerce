using MassTransit;

namespace BuildingBlocks.Messaging;

public sealed class MassTransitOutboxPublisher(IPublishEndpoint publishEndpoint) : IOutboxPublisher
{
    public Task PublishAsync(object integrationEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
}

using BuildingBlocks.Contracts;

namespace Ordering.Application.Abstractions;

public interface IOutboxWriter
{
    void Enqueue(IntegrationEvent integrationEvent);
}

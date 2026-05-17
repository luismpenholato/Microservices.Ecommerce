using BuildingBlocks.Contracts;

namespace Inventory.Application.Abstractions;

public interface IOutboxWriter
{
    void Enqueue(IntegrationEvent integrationEvent);
}

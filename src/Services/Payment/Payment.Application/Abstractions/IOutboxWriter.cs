using BuildingBlocks.Contracts;

namespace Payment.Application.Abstractions;

public interface IOutboxWriter
{
    void Enqueue(IntegrationEvent integrationEvent);
}

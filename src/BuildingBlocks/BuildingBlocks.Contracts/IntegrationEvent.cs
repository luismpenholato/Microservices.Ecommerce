namespace BuildingBlocks.Contracts;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

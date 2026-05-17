namespace BuildingBlocks.Contracts;

public sealed record OrderCompletedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
}

public sealed record OrderCancelledEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string Reason { get; init; }
}

public sealed record NotificationSentEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string Channel { get; init; }
    public required string Message { get; init; }
}

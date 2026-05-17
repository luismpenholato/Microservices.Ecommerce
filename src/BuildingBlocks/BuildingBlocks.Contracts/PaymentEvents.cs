namespace BuildingBlocks.Contracts;

public sealed record PaymentApprovedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string TransactionId { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

public sealed record PaymentRejectedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string Reason { get; init; }
}

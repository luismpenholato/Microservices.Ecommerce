namespace BuildingBlocks.Contracts;

public sealed record OrderCreatedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

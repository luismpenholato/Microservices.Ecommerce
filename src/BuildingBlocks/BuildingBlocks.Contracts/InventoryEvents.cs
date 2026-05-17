namespace BuildingBlocks.Contracts;

public sealed record StockReservedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required IReadOnlyList<ReservedStockItemDto> Items { get; init; }
}

public sealed record StockReservationFailedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string Reason { get; init; }
}

public sealed record ReservedStockItemDto
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}

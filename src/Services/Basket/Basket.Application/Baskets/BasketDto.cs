namespace Basket.Application.Baskets;

public sealed record BasketDto(
    Guid CustomerId,
    IReadOnlyList<BasketItemDto> Items,
    decimal Total);

public sealed record BasketItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

namespace Basket.Application.Checkout;

public sealed record CreateOrderRequest(
    string IdempotencyKey,
    Guid CustomerId,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record CheckoutResult(Guid OrderId, string Status);

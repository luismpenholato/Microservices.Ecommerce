using Basket.Application.Checkout;

namespace Basket.Application.Abstractions;

public interface IOrderingClient
{
    Task<CheckoutResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}

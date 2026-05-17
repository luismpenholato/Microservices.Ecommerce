using Basket.Application.Abstractions;
using Basket.Application.Checkout;
using MediatR;

namespace Basket.Application.Baskets.Commands;

public sealed record CheckoutBasketCommand(Guid CustomerId) : IRequest<CheckoutResult>;

public sealed class CheckoutBasketCommandHandler(
    IBasketRepository repository,
    IOrderingClient orderingClient) : IRequestHandler<CheckoutBasketCommand, CheckoutResult>
{
    public async Task<CheckoutResult> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)
    {
        var basket = await repository.GetAsync(request.CustomerId, cancellationToken);
        if (basket is null || basket.Items.Count == 0)
        {
            throw new InvalidOperationException("Basket is empty.");
        }

        var idempotencyKey = CheckoutIdempotencyKeyFactory.Create(request.CustomerId, basket);
        var orderRequest = new CreateOrderRequest(
            idempotencyKey,
            request.CustomerId,
            basket.Items.Select(x => new CreateOrderItemRequest(
                x.ProductId,
                x.ProductName,
                x.Quantity,
                x.UnitPrice)).ToList());

        var result = await orderingClient.CreateOrderAsync(orderRequest, cancellationToken);
        await repository.DeleteAsync(request.CustomerId, cancellationToken);
        return result;
    }
}

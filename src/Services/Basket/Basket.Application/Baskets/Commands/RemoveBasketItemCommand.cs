using Basket.Application.Abstractions;
using MediatR;

namespace Basket.Application.Baskets.Commands;

public sealed record RemoveBasketItemCommand(Guid CustomerId, Guid ProductId) : IRequest<BasketDto?>;

public sealed class RemoveBasketItemCommandHandler(IBasketRepository repository)
    : IRequestHandler<RemoveBasketItemCommand, BasketDto?>
{
    public async Task<BasketDto?> Handle(RemoveBasketItemCommand request, CancellationToken cancellationToken)
    {
        var basket = await repository.GetAsync(request.CustomerId, cancellationToken);
        if (basket is null)
        {
            return null;
        }

        if (!basket.RemoveItem(request.ProductId))
        {
            return null;
        }

        if (basket.Items.Count == 0)
        {
            await repository.DeleteAsync(request.CustomerId, cancellationToken);
            return new BasketDto(request.CustomerId, [], 0);
        }

        await repository.SaveAsync(basket, cancellationToken);
        return new BasketDto(
            basket.CustomerId,
            basket.Items.Select(x => new BasketItemDto(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList(),
            basket.Total);
    }
}

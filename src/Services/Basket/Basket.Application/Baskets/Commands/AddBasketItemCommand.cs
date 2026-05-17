using Basket.Application.Abstractions;
using MediatR;

namespace Basket.Application.Baskets.Commands;

public sealed record AddBasketItemCommand(
    Guid CustomerId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity) : IRequest<BasketDto>;

public sealed class AddBasketItemCommandHandler(IBasketRepository repository)
    : IRequestHandler<AddBasketItemCommand, BasketDto>
{
    public async Task<BasketDto> Handle(AddBasketItemCommand request, CancellationToken cancellationToken)
    {
        var basket = await repository.GetAsync(request.CustomerId, cancellationToken)
            ?? new Domain.Entities.CustomerBasket { CustomerId = request.CustomerId };

        basket.AddOrUpdateItem(request.ProductId, request.ProductName, request.UnitPrice, request.Quantity);
        await repository.SaveAsync(basket, cancellationToken);

        return Map(basket);
    }

    private static BasketDto Map(Domain.Entities.CustomerBasket basket) =>
        new(
            basket.CustomerId,
            basket.Items.Select(x => new BasketItemDto(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList(),
            basket.Total);
}

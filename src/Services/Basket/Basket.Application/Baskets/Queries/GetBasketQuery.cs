using Basket.Application.Abstractions;
using MediatR;

namespace Basket.Application.Baskets.Queries;

public sealed record GetBasketQuery(Guid CustomerId) : IRequest<BasketDto>;

public sealed class GetBasketQueryHandler(IBasketRepository repository)
    : IRequestHandler<GetBasketQuery, BasketDto>
{
    public async Task<BasketDto> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var basket = await repository.GetAsync(request.CustomerId, cancellationToken)
            ?? new Domain.Entities.CustomerBasket { CustomerId = request.CustomerId };

        return new BasketDto(
            basket.CustomerId,
            basket.Items.Select(x => new BasketItemDto(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList(),
            basket.Total);
    }
}

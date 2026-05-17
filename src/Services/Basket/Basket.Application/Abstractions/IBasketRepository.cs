using Basket.Domain.Entities;

namespace Basket.Application.Abstractions;

public interface IBasketRepository
{
    Task<CustomerBasket?> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task SaveAsync(CustomerBasket basket, CancellationToken cancellationToken);
    Task DeleteAsync(Guid customerId, CancellationToken cancellationToken);
}

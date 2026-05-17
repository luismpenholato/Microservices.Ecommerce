using Ordering.Application.Abstractions;
using Ordering.Application.Orders;
using MediatR;

namespace Ordering.Application.Orders.Queries;

public sealed record GetOrdersByCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrdersByCustomerQueryHandler(IOrderRepository repository)
    : IRequestHandler<GetOrdersByCustomerQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        return orders.Select(OrderMapper.ToDto).ToList();
    }
}

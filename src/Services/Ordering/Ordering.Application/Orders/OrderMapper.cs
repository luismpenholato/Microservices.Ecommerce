using Ordering.Domain.Entities;
using Ordering.Domain.Enums;

namespace Ordering.Application.Orders;

internal static class OrderMapper
{
    public static OrderDto ToDto(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Select(x => new OrderItemDto(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList());
}

using Inventory.Application.Abstractions;
using MediatR;

namespace Inventory.Application.Inventory.Queries;

public sealed record GetInventoryQuery(Guid ProductId) : IRequest<InventoryDto?>;

public sealed class GetInventoryQueryHandler(IInventoryRepository repository)
    : IRequestHandler<GetInventoryQuery, InventoryDto?>
{
    public async Task<InventoryDto?> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByProductIdAsync(request.ProductId, cancellationToken);
        return item is null ? null : new InventoryDto(item.ProductId, item.AvailableQuantity, item.ReservedQuantity);
    }
}

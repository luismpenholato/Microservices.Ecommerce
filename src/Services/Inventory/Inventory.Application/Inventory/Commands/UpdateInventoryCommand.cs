using Inventory.Application.Abstractions;
using MediatR;

namespace Inventory.Application.Inventory.Commands;

public sealed record UpdateInventoryCommand(Guid ProductId, int AvailableQuantity) : IRequest<InventoryDto>;

public sealed class UpdateInventoryCommandHandler(IInventoryRepository repository)
    : IRequestHandler<UpdateInventoryCommand, InventoryDto>
{
    public async Task<InventoryDto> Handle(UpdateInventoryCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (item is null)
        {
            item = new Domain.Entities.ProductInventory(request.ProductId, request.AvailableQuantity);
            await repository.AddAsync(item, cancellationToken);
        }
        else
        {
            item.UpdateAvailableQuantity(request.AvailableQuantity);
            await repository.UpdateAsync(item, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return new InventoryDto(item.ProductId, item.AvailableQuantity, item.ReservedQuantity);
    }
}

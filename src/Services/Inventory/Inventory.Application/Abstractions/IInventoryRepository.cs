using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IInventoryRepository
{
    Task<ProductInventory?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductInventory>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(ProductInventory inventory, CancellationToken cancellationToken);
    Task UpdateAsync(ProductInventory inventory, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

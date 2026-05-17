using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    public Task<ProductInventory?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        dbContext.ProductInventories.FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

    public async Task<IReadOnlyList<ProductInventory>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.ProductInventories.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(ProductInventory inventory, CancellationToken cancellationToken) =>
        await dbContext.ProductInventories.AddAsync(inventory, cancellationToken);

    public Task UpdateAsync(ProductInventory inventory, CancellationToken cancellationToken)
    {
        dbContext.ProductInventories.Update(inventory);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Persistence;

public static class InventoryDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryDbContext>>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.ProductInventories.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            new ProductInventory(Guid.Parse("11111111-1111-1111-1111-111111111101"), 25),
            new ProductInventory(Guid.Parse("11111111-1111-1111-1111-111111111102"), 80),
            new ProductInventory(Guid.Parse("11111111-1111-1111-1111-111111111103"), 40),
            new ProductInventory(Guid.Parse("11111111-1111-1111-1111-111111111104"), 120),
            new ProductInventory(Guid.Parse("11111111-1111-1111-1111-111111111105"), 60)
        };

        db.ProductInventories.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Inventory seed completed with {Count} products.", products.Length);
    }
}

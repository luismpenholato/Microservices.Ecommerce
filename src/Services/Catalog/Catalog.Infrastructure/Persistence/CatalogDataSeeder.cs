using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence;

public static class CatalogDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogDbContext>>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new[]
        {
            new Product(Guid.Parse("11111111-1111-1111-1111-111111111101"), "Notebook Pro", "Notebook para desenvolvimento", 5499.90m, 25),
            new Product(Guid.Parse("11111111-1111-1111-1111-111111111102"), "Teclado Mecânico", "Switch brown, layout ABNT2", 399.90m, 80),
            new Product(Guid.Parse("11111111-1111-1111-1111-111111111103"), "Monitor 27\"", "IPS 144Hz QHD", 1899.00m, 40),
            new Product(Guid.Parse("11111111-1111-1111-1111-111111111104"), "Mouse Ergonômico", "Sensor 26K DPI", 249.90m, 120),
            new Product(Guid.Parse("11111111-1111-1111-1111-111111111105"), "Headset Wireless", "Cancelamento de ruído", 699.00m, 60)
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Catalog seed completed with {Count} products.", products.Length);
    }
}

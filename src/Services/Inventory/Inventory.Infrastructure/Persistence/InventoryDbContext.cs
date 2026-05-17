using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<ProductInventory> ProductInventories => Set<ProductInventory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductInventory>(entity =>
        {
            entity.ToTable("product_inventories");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.AvailableQuantity);
            entity.Property(x => x.ReservedQuantity);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.EventType).HasMaxLength(200);
        });

        modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => new { x.EventId, x.ConsumerName });
            entity.Property(x => x.ConsumerName).HasMaxLength(200);
        });
    }
}

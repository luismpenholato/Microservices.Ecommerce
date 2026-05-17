using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence.Entities;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OrderIdempotencyRecord> OrderIdempotencyRecords => Set<OrderIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.OwnsMany(x => x.Items, items =>
            {
                items.ToTable("order_items");
                items.WithOwner().HasForeignKey("OrderId");
                items.HasKey("OrderId", nameof(OrderItem.ProductId));
                items.Property(x => x.ProductName).HasMaxLength(200);
                items.Property(x => x.UnitPrice).HasPrecision(18, 2);
            });
            entity.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => new { x.EventId, x.ConsumerName });
            entity.Property(x => x.ConsumerName).HasMaxLength(200);
            entity.Property(x => x.EventType).HasMaxLength(200);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => new { x.ProcessedAtUtc, x.RetryCount });
            entity.Property(x => x.EventType).HasMaxLength(200);
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
        });

        modelBuilder.Entity<OrderIdempotencyRecord>(entity =>
        {
            entity.ToTable("order_idempotency_records");
            entity.HasKey(x => x.IdempotencyKey);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
            entity.Property(x => x.RequestHash).HasMaxLength(128);
            entity.HasIndex(x => x.OrderId);
        });
    }
}

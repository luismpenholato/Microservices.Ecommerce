using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.EventType).HasMaxLength(200);
            entity.Property(x => x.LastError).HasMaxLength(2000);
        });

        modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => new { x.EventId, x.ConsumerName });
            entity.Property(x => x.ConsumerName).HasMaxLength(200);
            entity.Property(x => x.EventType).HasMaxLength(200);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure.Persistence.Entities;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => new { x.EventId, x.ConsumerName });
            entity.Property(x => x.ConsumerName).HasMaxLength(200);
        });
    }
}

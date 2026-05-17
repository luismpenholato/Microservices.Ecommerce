using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Outbox;

public sealed class OutboxPendingCountProvider(
    IServiceProvider serviceProvider,
    IOptions<OutboxOptions> options) : IOutboxPendingCountProvider
{
    public long GetPendingCount()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var maxRetries = options.Value.MaxPublishRetries;

        return db.OutboxMessages.Count(x => x.ProcessedAtUtc == null && x.RetryCount < maxRetries);
    }
}

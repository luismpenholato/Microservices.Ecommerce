using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ordering.Infrastructure.Persistence.Entities;

namespace Ordering.Infrastructure.Persistence;

public sealed class IntegrationEventUnitOfWork(
    OrderingDbContext dbContext,
    IntegrationEventUnitOfWorkExecutor executor,
    ILogger<IntegrationEventUnitOfWork> logger) : IIntegrationEventUnitOfWork
{
    public Task ExecuteIdempotentAsync<TEvent>(
        TEvent message,
        string consumerName,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent =>
        executor.ExecuteIdempotentAsync(
            message,
            consumerName,
            (eventId, name, ct) => dbContext.ProcessedIntegrationEvents
                .AnyAsync(x => x.EventId == eventId && x.ConsumerName == name, ct),
            async (work, ct) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    await work(ct);

                    dbContext.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
                    {
                        EventId = message.EventId,
                        ConsumerName = consumerName,
                        EventType = typeof(TEvent).Name,
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    logger.LogError(
                        ex,
                        "Ordering integration event transaction rolled back. ConsumerName={ConsumerName} EventId={EventId} CorrelationId={CorrelationId}",
                        consumerName,
                        message.EventId,
                        message.CorrelationId);
                    throw;
                }
            },
            handler,
            cancellationToken);
}

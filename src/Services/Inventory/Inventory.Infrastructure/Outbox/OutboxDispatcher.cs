using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    IServiceProvider serviceProvider,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(outboxOptions.Value.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inventory outbox dispatcher iteration failed.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<IOutboxMetricsRecorder>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<OutboxOptions>>();

        var maxRetries = options.Value.MaxPublishRetries;
        var batchSize = options.Value.BatchSize;

        var entities = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null && x.RetryCount < maxRetries)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return;
        }

        var entityMap = entities.ToDictionary(x => x.Id);
        var snapshots = entities.Select(Map).ToList();

        await OutboxDispatchLoop.DispatchBatchAsync(
            snapshots,
            publisher,
            metrics,
            options,
            logger,
            "InventoryService",
            async (snapshot, ct) =>
            {
                if (!entityMap.TryGetValue(snapshot.Id, out var entity))
                {
                    return;
                }

                entity.ProcessedAtUtc = snapshot.ProcessedAtUtc;
                entity.RetryCount = snapshot.RetryCount;
                entity.LastError = snapshot.LastError;
                await dbContext.SaveChangesAsync(ct);
            },
            cancellationToken);
    }

    private static OutboxDispatchLoop.OutboxMessageSnapshot Map(OutboxMessage message) => new()
    {
        Id = message.Id,
        EventId = message.EventId,
        EventType = message.EventType,
        Payload = message.Payload,
        CorrelationId = message.CorrelationId,
        ProcessedAtUtc = message.ProcessedAtUtc,
        RetryCount = message.RetryCount,
        LastError = message.LastError
    };
}

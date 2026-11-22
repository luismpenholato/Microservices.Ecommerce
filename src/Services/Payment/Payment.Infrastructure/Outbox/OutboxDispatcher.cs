using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Outbox;

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
                logger.LogError(ex, "Payment outbox dispatcher iteration failed.");
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
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<IOutboxMetricsRecorder>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<OutboxOptions>>();

        var maxRetries = options.Value.MaxPublishRetries;
        var batchSize = options.Value.BatchSize;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entities = await OutboxClaim.ClaimPendingAsync(
            dbContext.OutboxMessages,
            "outbox_messages",
            maxRetries,
            batchSize,
            cancellationToken);

        if (entities.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
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
            "PaymentService",
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

        await transaction.CommitAsync(cancellationToken);
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

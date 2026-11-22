using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Claims pending outbox rows with <c>FOR UPDATE SKIP LOCKED</c> so concurrent
/// dispatchers cannot publish the same message.
/// </summary>
public static class OutboxClaim
{
    public static string BuildClaimSql(string tableName) =>
        "SELECT * FROM " + tableName
        + " WHERE \"ProcessedAtUtc\" IS NULL"
        + " AND \"RetryCount\" < {0}"
        + " ORDER BY \"CreatedAtUtc\""
        + " LIMIT {1}"
        + " FOR UPDATE SKIP LOCKED";

    public static async Task<List<TMessage>> ClaimPendingAsync<TMessage>(
        DbSet<TMessage> outboxMessages,
        string tableName,
        int maxRetries,
        int batchSize,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        // Column names match EF default (PascalCase) used by Ordering/Payment/Inventory migrations.
        return await outboxMessages
            .FromSqlRaw(BuildClaimSql(tableName), maxRetries, batchSize)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using IdempotencyRecord = Ordering.Application.Abstractions.IdempotencyRecord;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence.Entities;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var record = await dbContext.OrderIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        return record is null
            ? null
            : await GetByIdAsync(record.OrderId, cancellationToken);
    }

    public async Task<IdempotencyRecord?> GetIdempotencyRecordAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.OrderIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        return record is null
            ? null
            : new IdempotencyRecord(record.IdempotencyKey, record.RequestHash, record.OrderId);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken) =>
        await dbContext.Orders.AddAsync(order, cancellationToken);

    public void AddIdempotencyRecord(string idempotencyKey, string requestHash, Guid orderId) =>
        dbContext.OrderIdempotencyRecords.Add(new OrderIdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            OrderId = orderId,
            CreatedAtUtc = DateTime.UtcNow
        });

    public Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

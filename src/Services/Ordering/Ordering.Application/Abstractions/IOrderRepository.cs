using Ordering.Domain.Entities;

namespace Ordering.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    void AddIdempotencyRecord(string idempotencyKey, string requestHash, Guid orderId);
    Task UpdateAsync(Order order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record IdempotencyRecord(string IdempotencyKey, string RequestHash, Guid OrderId);

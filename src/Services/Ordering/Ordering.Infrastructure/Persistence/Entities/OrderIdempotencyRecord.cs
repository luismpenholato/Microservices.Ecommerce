namespace Ordering.Infrastructure.Persistence.Entities;

public sealed class OrderIdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

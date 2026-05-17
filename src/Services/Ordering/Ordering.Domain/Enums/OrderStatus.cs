namespace Ordering.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    PaymentApproved = 1,
    PaymentRejected = 2,
    StockReserved = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

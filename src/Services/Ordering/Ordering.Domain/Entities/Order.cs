using BuildingBlocks.Domain;
using Ordering.Domain.Enums;

namespace Ordering.Domain.Entities;

public sealed class Order : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public Order(Guid id, Guid customerId, IEnumerable<OrderItem> items)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        _items.AddRange(items);
    }

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(x => x.UnitPrice * x.Quantity);

    public void MarkPaymentApproved()
    {
        if (Status is OrderStatus.PaymentApproved or OrderStatus.StockReserved or OrderStatus.Completed)
        {
            return;
        }

        EnsureStatus(OrderStatus.Pending);
        Status = OrderStatus.PaymentApproved;
        MarkUpdated();
    }

    public void MarkPaymentRejected()
    {
        if (Status is OrderStatus.PaymentRejected or OrderStatus.Cancelled or OrderStatus.Completed)
        {
            return;
        }

        Status = OrderStatus.PaymentRejected;
        MarkUpdated();
    }

    public void MarkStockReserved()
    {
        if (Status is OrderStatus.StockReserved or OrderStatus.Completed)
        {
            return;
        }

        EnsureStatus(OrderStatus.PaymentApproved);
        Status = OrderStatus.StockReserved;
        MarkUpdated();
    }

    public void MarkFailed()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            return;
        }

        Status = OrderStatus.Failed;
        MarkUpdated();
    }

    public void MarkCompleted()
    {
        if (Status == OrderStatus.Completed)
        {
            return;
        }

        EnsureStatus(OrderStatus.StockReserved);
        Status = OrderStatus.Completed;
        MarkUpdated();
    }

    public void MarkCancelled()
    {
        if (Status is OrderStatus.Completed)
        {
            throw new InvalidOperationException("Completed orders cannot be cancelled.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        Status = OrderStatus.Cancelled;
        MarkUpdated();
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Order must be in {expected} status. Current: {Status}");
        }
    }
}

public sealed class OrderItem
{
    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}

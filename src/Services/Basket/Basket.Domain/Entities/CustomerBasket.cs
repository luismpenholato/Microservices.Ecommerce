namespace Basket.Domain.Entities;

public sealed class CustomerBasket
{
    public required Guid CustomerId { get; init; }
    public List<BasketItem> Items { get; } = [];

    public decimal Total => Items.Sum(x => x.UnitPrice * x.Quantity);

    public void AddOrUpdateItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        var existing = Items.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            Items.Add(new BasketItem
            {
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity
            });
            return;
        }

        existing.Quantity += quantity;
    }

    public bool RemoveItem(Guid productId) => Items.RemoveAll(x => x.ProductId == productId) > 0;
}

public sealed class BasketItem
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; set; }
    public required decimal UnitPrice { get; set; }
    public required int Quantity { get; set; }
}

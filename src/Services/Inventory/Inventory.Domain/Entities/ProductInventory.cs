namespace Inventory.Domain.Entities;

public sealed class ProductInventory
{
    public Guid ProductId { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public uint RowVersion { get; set; }

    private ProductInventory() { }

    public ProductInventory(Guid productId, int availableQuantity)
    {
        ProductId = productId;
        AvailableQuantity = availableQuantity;
    }

    public void UpdateAvailableQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        AvailableQuantity = quantity;
    }

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0 || AvailableQuantity < quantity)
        {
            return false;
        }

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
        return true;
    }
}

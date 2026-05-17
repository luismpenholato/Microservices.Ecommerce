namespace Inventory.Application.Inventory;

public sealed record InventoryDto(Guid ProductId, int AvailableQuantity, int ReservedQuantity);

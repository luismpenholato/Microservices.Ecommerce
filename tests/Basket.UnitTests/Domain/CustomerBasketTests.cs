using Basket.Domain.Entities;
using FluentAssertions;

namespace Basket.UnitTests.Domain;

public class CustomerBasketTests
{
    [Fact]
    public void AddOrUpdateItem_Should_Increase_Quantity_For_Same_Product()
    {
        var basket = new CustomerBasket { CustomerId = Guid.NewGuid() };
        var productId = Guid.NewGuid();

        basket.AddOrUpdateItem(productId, "Item", 10m, 2);
        basket.AddOrUpdateItem(productId, "Item", 10m, 3);

        basket.Items.Should().ContainSingle();
        basket.Items.First().Quantity.Should().Be(5);
        basket.Total.Should().Be(50m);
    }

    [Fact]
    public void RemoveItem_Should_Return_True_When_Item_Exists()
    {
        var basket = new CustomerBasket { CustomerId = Guid.NewGuid() };
        var productId = Guid.NewGuid();
        basket.AddOrUpdateItem(productId, "Item", 10m, 1);

        basket.RemoveItem(productId).Should().BeTrue();
        basket.Items.Should().BeEmpty();
    }
}

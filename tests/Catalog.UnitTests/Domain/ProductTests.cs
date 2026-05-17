using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Update_Should_Change_All_Fields()
    {
        var product = new Product(Guid.NewGuid(), "A", "Desc", 10m, 5);

        product.Update("B", "New", 20m, 8);

        product.Name.Should().Be("B");
        product.Description.Should().Be("New");
        product.Price.Should().Be(20m);
        product.StockQuantity.Should().Be(8);
        product.UpdatedAtUtc.Should().NotBeNull();
    }
}

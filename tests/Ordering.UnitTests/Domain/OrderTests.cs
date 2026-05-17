using FluentAssertions;
using Ordering.Domain.Entities;
using Ordering.Domain.Enums;

namespace Ordering.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void MarkPaymentApproved_Should_Transition_From_Pending()
    {
        var order = CreateOrder();

        order.MarkPaymentApproved();

        order.Status.Should().Be(OrderStatus.PaymentApproved);
    }

    [Fact]
    public void MarkPaymentApproved_Should_Be_Idempotent_When_Already_Approved()
    {
        var order = CreateOrder();
        order.MarkPaymentApproved();

        var act = () => order.MarkPaymentApproved();

        act.Should().NotThrow();
        order.Status.Should().Be(OrderStatus.PaymentApproved);
    }

    [Fact]
    public void MarkCompleted_Should_Require_StockReserved()
    {
        var order = CreateOrder();
        order.MarkPaymentApproved();
        order.MarkStockReserved();

        order.MarkCompleted();

        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void MarkPaymentApproved_Should_Throw_When_Already_Rejected()
    {
        var order = CreateOrder();
        order.MarkPaymentRejected();

        var act = () => order.MarkPaymentApproved();

        act.Should().Throw<InvalidOperationException>();
    }

    private static Order CreateOrder() =>
        new(Guid.NewGuid(), Guid.NewGuid(), [new OrderItem(Guid.NewGuid(), "P", 1, 10m)]);
}

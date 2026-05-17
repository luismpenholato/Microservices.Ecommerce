using BuildingBlocks.Contracts;

namespace BuildingBlocks.Messaging;

public static class IntegrationEventLogging
{
    public static Guid? TryGetOrderId(IntegrationEvent integrationEvent) =>
        integrationEvent switch
        {
            OrderCreatedEvent e => e.OrderId,
            PaymentApprovedEvent e => e.OrderId,
            PaymentRejectedEvent e => e.OrderId,
            StockReservedEvent e => e.OrderId,
            StockReservationFailedEvent e => e.OrderId,
            OrderCompletedEvent e => e.OrderId,
            OrderCancelledEvent e => e.OrderId,
            _ => null
        };
}

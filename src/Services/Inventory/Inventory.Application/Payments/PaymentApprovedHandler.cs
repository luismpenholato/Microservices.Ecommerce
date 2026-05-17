using BuildingBlocks.Contracts;
using Inventory.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Payments;

public sealed class PaymentApprovedHandler(
    IInventoryRepository repository,
    IOutboxWriter outboxWriter,
    IStockReservationMetrics stockMetrics,
    ILogger<PaymentApprovedHandler> logger)
{
    public async Task HandleAsync(PaymentApprovedEvent message, CancellationToken cancellationToken)
    {
        var reservedItems = new List<ReservedStockItemDto>();
        var failedProducts = new List<Guid>();

        foreach (var item in message.Items)
        {
            var inventory = await repository.GetByProductIdAsync(item.ProductId, cancellationToken);
            if (inventory is null || !inventory.TryReserve(item.Quantity))
            {
                failedProducts.Add(item.ProductId);
                continue;
            }

            await repository.UpdateAsync(inventory, cancellationToken);
            reservedItems.Add(new ReservedStockItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });
        }

        if (failedProducts.Count > 0)
        {
            outboxWriter.Enqueue(new StockReservationFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                Reason = $"Insufficient stock for products: {string.Join(',', failedProducts)}"
            });

            stockMetrics.RecordReservationFailed();

            logger.LogWarning(
                "Stock reservation failed for order {OrderId}. EventId={EventId} CorrelationId={CorrelationId}",
                message.OrderId,
                message.EventId,
                message.CorrelationId);
            return;
        }

        outboxWriter.Enqueue(new StockReservedEvent
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            Items = reservedItems
        });

        stockMetrics.RecordReservationApproved();

        logger.LogInformation(
            "Stock reserved for order {OrderId}. EventId={EventId} CorrelationId={CorrelationId}",
            message.OrderId,
            message.EventId,
            message.CorrelationId);
    }
}

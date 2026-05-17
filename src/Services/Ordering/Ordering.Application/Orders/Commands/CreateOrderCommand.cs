using BuildingBlocks.Contracts;
using Ordering.Application.Abstractions;
using Ordering.Application.Exceptions;
using Ordering.Application.Orders;
using Ordering.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ordering.Application.Orders.Commands;

public sealed record CreateOrderCommand(
    string IdempotencyKey,
    Guid CustomerId,
    IReadOnlyList<CreateOrderItem> Items,
    Guid CorrelationId) : IRequest<OrderDto>;

public sealed record CreateOrderItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed class CreateOrderCommandHandler(
    IOrderRepository repository,
    IOutboxWriter outboxWriter,
    IOrderMetrics orderMetrics,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var requestHash = OrderRequestHasher.Compute(request);
        var existingRecord = await repository.GetIdempotencyRecordAsync(request.IdempotencyKey, cancellationToken);

        if (existingRecord is not null)
        {
            if (!string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException(request.IdempotencyKey);
            }

            var existingOrder = await repository.GetByIdAsync(existingRecord.OrderId, cancellationToken);
            if (existingOrder is not null)
            {
                logger.LogInformation(
                    "Returning existing order {OrderId} for idempotency key {IdempotencyKey}",
                    existingOrder.Id,
                    request.IdempotencyKey);
                return OrderMapper.ToDto(existingOrder);
            }
        }

        var orderId = Guid.NewGuid();
        var items = request.Items.Select(x =>
            new OrderItem(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice));

        var order = new Order(orderId, request.CustomerId, items);
        await repository.AddAsync(order, cancellationToken);
        repository.AddIdempotencyRecord(request.IdempotencyKey, requestHash, order.Id);

        outboxWriter.Enqueue(new OrderCreatedEvent
        {
            CorrelationId = request.CorrelationId,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(x =>
                new BuildingBlocks.Contracts.OrderItemDto(
                    x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList()
        });

        await repository.SaveChangesAsync(cancellationToken);

        orderMetrics.RecordOrderCreated();

        logger.LogInformation(
            "Order {OrderId} created. Outbox enqueued OrderCreatedEvent. CorrelationId={CorrelationId} IdempotencyKey={IdempotencyKey}",
            order.Id,
            request.CorrelationId,
            request.IdempotencyKey);

        return OrderMapper.ToDto(order);
    }
}

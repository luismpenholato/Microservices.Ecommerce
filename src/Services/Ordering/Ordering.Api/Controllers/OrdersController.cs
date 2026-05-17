using BuildingBlocks.Web;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api;
using Ordering.Application.Orders.Commands;
using Ordering.Application.Orders.Queries;

namespace Ordering.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(IMediator mediator, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (currentUser.CustomerId != order.CustomerId)
        {
            return Forbid();
        }

        return Ok(order);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var accessError = this.EnsureRouteCustomerMatchesToken(customerId, currentUser);
        if (accessError is not null)
        {
            return accessError;
        }

        var orders = await mediator.Send(new GetOrdersByCustomerQuery(customerId), cancellationToken);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CustomerId is null)
        {
            return Unauthorized();
        }

        var idempotencyKey = Request.Headers[IdempotencyKeyConstants.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { title = "Idempotency-Key header is required." });
        }

        var order = await mediator.Send(
            new CreateOrderCommand(
                idempotencyKey,
                currentUser.CustomerId.Value,
                request.Items.Select(x => new CreateOrderItem(
                    x.ProductId,
                    x.ProductName,
                    x.Quantity,
                    x.UnitPrice)).ToList(),
                CorrelationIdHelper.FromHttpContext(HttpContext)),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}

public sealed record CreateOrderRequest(IReadOnlyList<CreateOrderItemRequest> Items);
public sealed record CreateOrderItemRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

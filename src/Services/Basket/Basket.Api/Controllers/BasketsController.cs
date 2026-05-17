using Basket.Application.Baskets.Commands;
using Basket.Application.Baskets.Queries;
using BuildingBlocks.Web;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Basket.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/baskets")]
public sealed class BasketsController(IMediator mediator, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var accessError = this.EnsureRouteCustomerMatchesToken(customerId, currentUser);
        if (accessError is not null)
        {
            return accessError;
        }

        var basket = await mediator.Send(new GetBasketQuery(customerId), cancellationToken);
        return Ok(basket);
    }

    [HttpPost("{customerId:guid}/items")]
    public async Task<IActionResult> AddItem(
        Guid customerId,
        [FromBody] AddBasketItemRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = this.EnsureRouteCustomerMatchesToken(customerId, currentUser);
        if (accessError is not null)
        {
            return accessError;
        }

        var basket = await mediator.Send(
            new AddBasketItemCommand(customerId, request.ProductId, request.ProductName, request.UnitPrice, request.Quantity),
            cancellationToken);
        return Ok(basket);
    }

    [HttpDelete("{customerId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid customerId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var accessError = this.EnsureRouteCustomerMatchesToken(customerId, currentUser);
        if (accessError is not null)
        {
            return accessError;
        }

        var basket = await mediator.Send(new RemoveBasketItemCommand(customerId, productId), cancellationToken);
        return basket is null ? NotFound() : Ok(basket);
    }

    [HttpPost("{customerId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid customerId, CancellationToken cancellationToken)
    {
        var accessError = this.EnsureRouteCustomerMatchesToken(customerId, currentUser);
        if (accessError is not null)
        {
            return accessError;
        }

        var result = await mediator.Send(new CheckoutBasketCommand(customerId), cancellationToken);
        return Ok(result);
    }
}

public sealed record AddBasketItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

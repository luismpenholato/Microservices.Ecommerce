using Inventory.Application.Inventory.Commands;
using Inventory.Application.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(IMediator mediator) : ControllerBase
{
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(Guid productId, CancellationToken cancellationToken)
    {
        var item = await mediator.Send(new GetInventoryQuery(productId), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var item = await mediator.Send(
            new UpdateInventoryCommand(productId, request.AvailableQuantity),
            cancellationToken);
        return Ok(item);
    }
}

public sealed record UpdateInventoryRequest(int AvailableQuantity);

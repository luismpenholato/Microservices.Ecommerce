using BuildingBlocks.Web;
using Catalog.Application.Products.Commands;
using Catalog.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await mediator.Send(
            new CreateProductCommand(request.Name, request.Description, request.Price, request.StockQuantity),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await mediator.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.StockQuantity),
            cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}

public sealed record CreateProductRequest(string Name, string Description, decimal Price, int StockQuantity);
public sealed record UpdateProductRequest(string Name, string Description, decimal Price, int StockQuantity);

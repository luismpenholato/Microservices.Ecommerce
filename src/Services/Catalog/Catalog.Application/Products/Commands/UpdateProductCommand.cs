using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Products.Commands;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity) : IRequest<ProductDto?>;

public sealed class UpdateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Update(request.Name, request.Description, request.Price, request.StockQuantity);
        await repository.UpdateAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
    }
}

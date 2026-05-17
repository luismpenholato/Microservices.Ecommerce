using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public sealed class GetProductByIdQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        return product is null
            ? null
            : new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
    }
}

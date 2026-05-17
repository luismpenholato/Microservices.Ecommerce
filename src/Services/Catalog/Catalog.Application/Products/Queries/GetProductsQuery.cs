using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Products.Queries;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return products.Select(Map).ToList();
    }

    private static ProductDto Map(Domain.Entities.Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
}

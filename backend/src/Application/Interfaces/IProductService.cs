using Application.Common;
using Application.DTOs.Products;

namespace Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> SearchAsync(ProductListQuery query, CancellationToken cancellationToken);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken);
}

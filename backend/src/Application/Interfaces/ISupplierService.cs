using Application.Common;
using Application.DTOs.Suppliers;

namespace Application.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> SearchAsync(SupplierListQuery query, CancellationToken cancellationToken);

    Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken);

    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken);
}

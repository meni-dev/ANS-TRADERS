using Application.Common;
using Application.DTOs.Customers;

namespace Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> SearchAsync(CustomerListQuery query, CancellationToken cancellationToken);

    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);

    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken);
}

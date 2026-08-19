using Domain.Entities;

namespace Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

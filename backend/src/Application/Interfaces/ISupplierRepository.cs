using Domain.Entities;

namespace Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Supplier> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

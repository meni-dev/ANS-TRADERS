using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IPurchaseRepository
{
    /// <summary>Loads the document with its lines. Used by the detail screen and by cancellation.</summary>
    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Purchase> Items, int TotalCount)> SearchAsync(
        string? search,
        PurchaseStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? supplierId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Highest sequence already used in the given financial year, or 0 if none.</summary>
    Task<int> GetLastSequenceAsync(string financialYear, CancellationToken cancellationToken);

    Task<bool> SupplierInvoiceNumberExistsAsync(
        Guid supplierId, string supplierInvoiceNumber, CancellationToken cancellationToken);

    Task AddAsync(Purchase purchase, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

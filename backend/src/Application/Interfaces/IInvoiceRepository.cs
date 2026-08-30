using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IInvoiceRepository
{
    /// <summary>Loads the document with its lines. Used by the printed bill and by cancellation.</summary>
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> SearchAsync(
        string? search,
        InvoiceStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? customerId,
        bool? unpaidOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Highest sequence already used in the given financial year, or 0 if none.</summary>

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

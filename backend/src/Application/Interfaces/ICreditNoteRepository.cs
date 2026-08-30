using Domain.Entities;

namespace Application.Interfaces;

public interface ICreditNoteRepository
{
    /// <summary>Loads the note with its lines, tracked — cancellation moves every one of them.</summary>
    Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<CreditNote> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? invoiceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Highest sequence used in the given financial year, or 0. Its own series.</summary>

    /// <summary>Every sequence used in a year, for the dashboard's gap check.</summary>
    Task<IReadOnlyList<int>> GetSequencesAsync(string financialYear, CancellationToken cancellationToken);

    /// <summary>
    /// Whether any live note exists against a bill. Cancelling a bill that has been partly returned
    /// would put the same goods back on the shelf twice, so it is refused.
    /// </summary>
    Task<bool> HasLiveNotesForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task AddAsync(CreditNote note, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

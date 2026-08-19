using Domain.Entities;

namespace Application.Interfaces;

public interface IDebitNoteRepository
{
    /// <summary>Loads the note with its lines, tracked — cancellation moves every one of them.</summary>
    Task<DebitNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DebitNote> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? supplierId,
        Guid? purchaseId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Highest sequence used in the given financial year, or 0. Its own series.</summary>
    Task<int> GetLastSequenceAsync(string financialYear, CancellationToken cancellationToken);

    /// <summary>Every sequence used in a year, for the dashboard's gap check.</summary>
    Task<IReadOnlyList<int>> GetSequencesAsync(string financialYear, CancellationToken cancellationToken);

    /// <summary>
    /// Whether any live note exists against a bill. Cancelling a supplier bill that has been partly returned
    /// would take the same goods off the shelf twice, so it is refused.
    /// </summary>
    Task<bool> HasLiveNotesForPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken);

    Task AddAsync(DebitNote note, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

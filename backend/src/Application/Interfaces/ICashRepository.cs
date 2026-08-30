using Application.DTOs.Cash;
using Domain.Entities;

namespace Application.Interfaces;

public interface ICashRepository
{
    Task<DayClose?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>The most recent close on or before a date — where the day's opening comes from.</summary>
    Task<DayClose?> GetLastCloseOnOrBeforeAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// The most recent day close in the book, whatever its date. Everything on or before it has
    /// been counted — a close carries its opening forward from the one before, so a day nobody
    /// closed individually still sits inside a counted stretch.
    /// </summary>
    Task<DayClose?> GetLatestCloseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cash in and out over a range, from every document that moves the drawer: receipts, payments,
    /// refunds and expenses. Returned unordered — the service sorts and runs the balance.
    /// </summary>
    Task<IReadOnlyList<CashBookEntryDto>> GetCashMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Every close inside a range, oldest first — the cash book's reconciliation points.</summary>
    Task<IReadOnlyList<DayClose>> GetClosesBetweenAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task AddAsync(DayClose close, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

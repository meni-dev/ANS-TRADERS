using Application.Common.Exceptions;
using Application.DTOs.Cash;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace UnitTests;

/// <summary>Answers with whatever close the test set up; everything else is out of scope.</summary>
internal sealed class FakeCashRepository : ICashRepository
{
    public DayClose? Latest { get; set; }

    public Task<DayClose?> GetLatestCloseAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Latest);

    public Task<DayClose?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<DayClose?> GetLastCloseOnOrBeforeAsync(DateOnly date, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<CashBookEntryDto>> GetCashMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<DayClose>> GetClosesBetweenAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task AddAsync(DayClose close, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class CashDayLockTests
{
    private static (CashDayLock Lock, FakeCashRepository Cash) Build(DateOnly? lastClosed)
    {
        var cash = new FakeCashRepository
        {
            Latest = lastClosed is { } date ? new DayClose { CloseDate = date } : null,
        };

        return (new CashDayLock(cash), cash);
    }

    [Fact]
    public async Task A_shop_that_has_never_closed_a_day_is_never_blocked()
    {
        var (dayLock, _) = Build(null);

        await dayLock.GuardAsync(new DateOnly(2026, 8, 25), "receipt", movesCash: true, CancellationToken.None);
    }

    /// <summary>
    /// The one that matters most in practice. A shop closes its first day and then keeps trading;
    /// if the lock read "the last close before this date" it would refuse every cash entry from
    /// that moment on, which is the opposite of what it is for.
    /// </summary>
    [Fact]
    public async Task Cash_after_the_last_close_is_allowed()
    {
        var (dayLock, _) = Build(new DateOnly(2026, 8, 12));

        await dayLock.GuardAsync(new DateOnly(2026, 8, 25), "receipt", movesCash: true, CancellationToken.None);
    }

    [Fact]
    public async Task Cash_on_the_closed_day_itself_is_refused()
    {
        var (dayLock, _) = Build(new DateOnly(2026, 8, 12));

        await Assert.ThrowsAsync<ConflictException>(() =>
            dayLock.GuardAsync(new DateOnly(2026, 8, 12), "receipt", movesCash: true, CancellationToken.None));
    }

    /// <summary>
    /// A day nobody closed individually but that sits behind a later close is still counted: the
    /// close carried its opening forward through it, so money added there moves a figure somebody
    /// already agreed.
    /// </summary>
    [Fact]
    public async Task Cash_inside_an_uncounted_gap_behind_a_close_is_refused()
    {
        var (dayLock, _) = Build(new DateOnly(2026, 8, 12));

        await Assert.ThrowsAsync<ConflictException>(() =>
            dayLock.GuardAsync(new DateOnly(2026, 8, 3), "receipt", movesCash: true, CancellationToken.None));
    }

    /// <summary>
    /// A credit bill, a UPI receipt or a bank transfer on a closed day changes nothing about what
    /// was in the drawer, so the lock has no business refusing them.
    /// </summary>
    [Fact]
    public async Task An_entry_that_moves_no_cash_passes_whatever_the_date()
    {
        var (dayLock, _) = Build(new DateOnly(2026, 8, 12));

        await dayLock.GuardAsync(new DateOnly(2026, 8, 1), "bill", movesCash: false, CancellationToken.None);
    }
}

using Application.Common.Exceptions;
using Application.DTOs.Cash;
using Application.Interfaces;
using Domain.Enums;
using Domain.Entities;

namespace Application.Services;

public class CashService : ICashService
{
    private readonly ICashRepository _repository;
    private readonly IPeriodLock _periodLock;
    private readonly ICurrentUser _currentUser;

    private readonly IShopClock _clock;

    public CashService(ICashRepository repository, IPeriodLock periodLock, ICurrentUser currentUser, IShopClock clock)
    {
        _repository = repository;
        _periodLock = periodLock;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CashPositionDto> GetPositionAsync(
        DateOnly date, CancellationToken cancellationToken)
    {
        var closed = await _repository.GetCloseAsync(date, cancellationToken);

        if (closed is not null)
        {
            // A closed day answers from its own snapshot. Recomputing would let a receipt entered
            // afterwards silently rewrite a count somebody signed off.
            return new CashPositionDto(
                date, closed.OpeningCash, closed.CashReceived, closed.CashPaidOut,
                closed.CashExpenses, closed.ExpectedCash, true, true,
                closed.CountedCash, closed.Difference, closed.Reason);
        }

        var (opening, carried) = await OpeningForAsync(date, cancellationToken);
        var day = await MovementsForAsync(date, cancellationToken);

        return new CashPositionDto(
            date, opening, day.Received, day.PaidOut, day.Expenses,
            Round(opening + day.Received - day.PaidOut - day.Expenses),
            carried, false, null, null, null);
    }

    public async Task<DayCloseDto> CloseDayAsync(
        CloseDayRequest request, CancellationToken cancellationToken)
    {
        if (request.CloseDate > _clock.Today)
        {
            throw Invalid("CloseDate", "A day cannot be closed before it has happened");
        }

        _currentUser.Require(Permission.CashDayClose, "close the day");

        await _periodLock.GuardAsync(request.CloseDate, "day close", cancellationToken);

        if (await _repository.GetCloseAsync(request.CloseDate, cancellationToken) is not null)
        {
            throw new ConflictException(
                $"{request.CloseDate:dd MMM yyyy} has already been closed", "DAY_ALREADY_CLOSED");
        }

        var counted = Round(request.CountedCash);

        if (counted < 0)
        {
            throw Invalid("CountedCash", "A drawer cannot hold less than nothing");
        }

        var (opening, _) = await OpeningForAsync(request.CloseDate, cancellationToken);
        var day = await MovementsForAsync(request.CloseDate, cancellationToken);

        var expected = Round(opening + day.Received - day.PaidOut - day.Expenses);
        var difference = Round(counted - expected);

        // A difference either way needs a sentence. An unexplained surplus is as much a sign of a
        // mis-keyed bill as a shortage is of a missing note, and both are worth finding on the day
        // rather than at the year end.
        if (difference != 0 && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw Invalid(
                "Reason",
                difference < 0
                    ? $"The drawer is {Math.Abs(difference):0.00} short — say why before closing"
                    : $"The drawer is {difference:0.00} over — say why before closing");
        }

        var close = new DayClose
        {
            CloseDate = request.CloseDate,
            OpeningCash = opening,
            CashReceived = day.Received,
            CashPaidOut = day.PaidOut,
            CashExpenses = day.Expenses,
            ExpectedCash = expected,
            CountedCash = counted,
            Difference = difference,
            Reason = Clean(request.Reason),
            Notes = Clean(request.Notes),
        };

        await _repository.AddAsync(close, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return ToDto(close);
    }

    public async Task<CashBookDto> GetCashBookAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var (opening, _) = await OpeningForAsync(fromDate, cancellationToken);

        var movements = await _repository.GetCashMovementsAsync(fromDate, toDate, cancellationToken);

        var closes = (await _repository.GetClosesBetweenAsync(fromDate, toDate, cancellationToken))
            .ToDictionary(c => c.CloseDate);

        var balance = opening;
        var running = new List<CashBookEntryDto>(movements.Count + closes.Count);

        // Ordered here rather than in the query: the entries come from two tables and the running
        // balance only means anything once they are interleaved by date.
        var ordered = movements
            .OrderBy(e => e.Date).ThenBy(e => e.Kind).ThenBy(e => e.Reference)
            .ToList();

        DateOnly? lastDate = null;

        foreach (var entry in ordered)
        {
            // A close is a reconciliation point, like a bank statement: from it on, the book runs
            // from what was physically counted, not from what it had computed. Without this the
            // book and the drawer drift apart by every difference ever explained — and then two
            // screens in the same app give two answers to "how much cash is there".
            if (lastDate is { } previous && entry.Date > previous)
            {
                SettleClosesUpTo(previous, entry.Date);
            }

            balance = Round(balance + entry.In - entry.Out);
            running.Add(entry with { Balance = balance });
            lastDate = entry.Date;
        }

        SettleClosesUpTo(lastDate ?? fromDate.AddDays(-1), toDate.AddDays(1));

        return new CashBookDto(fromDate, toDate, opening, balance, running);

        void SettleClosesUpTo(DateOnly after, DateOnly before)
        {
            foreach (var (date, close) in closes.Where(c => c.Key >= after && c.Key < before)
                         .OrderBy(c => c.Key))
            {
                if (close.Difference != 0)
                {
                    running.Add(new CashBookEntryDto(
                        date,
                        "Day close",
                        date.ToString("dd MMM"),
                        close.Difference < 0
                            ? $"Counted short — {close.Reason}"
                            : $"Counted over — {close.Reason}",
                        close.Difference > 0 ? close.Difference : 0m,
                        close.Difference < 0 ? -close.Difference : 0m,
                        close.CountedCash));
                }

                balance = close.CountedCash;
            }
        }
    }

    /// <summary>
    /// What the drawer held when the day started.
    /// <para>
    /// Taken from the last close's <b>counted</b> figure, not its expected one: whatever the book
    /// said, the notes that were physically there are what the next day starts with. Days between
    /// that close and this one are added on, so skipping a close does not lose their cash — the flag
    /// says the figure was carried rather than counted.
    /// </para>
    /// </summary>
    private async Task<(decimal Opening, bool Carried)> OpeningForAsync(
        DateOnly date, CancellationToken cancellationToken)
    {
        var previous = date.AddDays(-1);
        var last = await _repository.GetLastCloseOnOrBeforeAsync(previous, cancellationToken);

        if (last is null)
        {
            // Nothing has ever been closed, so everything the app has seen is still notionally in
            // the drawer. Honest, and it stops the first close being wrong by the whole history.
            var all = await MovementsBetweenAsync(new DateOnly(2000, 1, 1), previous, cancellationToken);
            return (Round(all.Received - all.PaidOut - all.Expenses), true);
        }

        if (last.CloseDate == previous)
        {
            return (last.CountedCash, false);
        }

        var since = await MovementsBetweenAsync(last.CloseDate.AddDays(1), previous, cancellationToken);
        return (Round(last.CountedCash + since.Received - since.PaidOut - since.Expenses), true);
    }

    private Task<(decimal Received, decimal PaidOut, decimal Expenses)> MovementsForAsync(
        DateOnly date, CancellationToken cancellationToken) =>
        MovementsBetweenAsync(date, date, cancellationToken);

    private async Task<(decimal Received, decimal PaidOut, decimal Expenses)> MovementsBetweenAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
        {
            return (0m, 0m, 0m);
        }

        var movements = await _repository.GetCashMovementsAsync(fromDate, toDate, cancellationToken);

        return (
            Round(movements.Where(m => m.Kind == "Receipt").Sum(m => m.In)),
            Round(movements.Where(m => m.Kind == "Paid out").Sum(m => m.Out)),
            Round(movements.Where(m => m.Kind == "Expense").Sum(m => m.Out)));
    }

    private static DayCloseDto ToDto(DayClose d) => new(
        d.Id, d.CloseDate, d.OpeningCash, d.CashReceived, d.CashPaidOut, d.CashExpenses,
        d.ExpectedCash, d.CountedCash, d.Difference, d.Reason, d.Notes, d.CreatedAt);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

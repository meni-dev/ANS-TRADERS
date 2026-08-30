using Application.Common.Exceptions;
using Application.Interfaces;

namespace Application.Services;

public class CashDayLock : ICashDayLock
{
    private readonly ICashRepository _cash;

    public CashDayLock(ICashRepository cash)
    {
        _cash = cash;
    }

    public async Task GuardAsync(
        DateOnly entryDate,
        string what,
        bool movesCash,
        CancellationToken cancellationToken)
    {
        if (!movesCash)
        {
            return;
        }

        // The latest close in the book, then a comparison — not "the last close before this date",
        // which would refuse everything ever entered after the shop's first close.
        //
        // On or before, not equal to: a close carries its opening forward from the previous one, so
        // a receipt dated inside an uncounted gap still changes what a later close's opening should
        // have been.
        var close = await _cash.GetLatestCloseAsync(cancellationToken);

        if (close is null || entryDate > close.CloseDate)
        {
            return;
        }

        // A conflict, not a validation error: the request is well formed and the date may well be
        // the right one. What has changed is the state of the books.
        throw new ConflictException(
            $"{close.CloseDate:dd MMM yyyy} has already been counted and closed, so this {what} " +
            $"dated {entryDate:dd MMM yyyy} cannot move cash into it. Date it today, or reopen the " +
            "day close first.",
            "CASH_DAY_CLOSED");
    }
}

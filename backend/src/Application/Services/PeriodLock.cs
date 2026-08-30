using Application.Common.Exceptions;
using Application.Interfaces;

namespace Application.Services;

public class PeriodLock : IPeriodLock
{
    private readonly IShopSettingsRepository _settings;

    public PeriodLock(IShopSettingsRepository settings)
    {
        _settings = settings;
    }

    public Task GuardAsync(DateOnly documentDate, string what, CancellationToken cancellationToken) =>
        GuardAsync(documentDate, what, applyFloor: true, cancellationToken);

    public Task GuardUndoAsync(DateOnly documentDate, string what, CancellationToken cancellationToken) =>
        GuardAsync(documentDate, what, applyFloor: false, cancellationToken);

    private async Task GuardAsync(
        DateOnly documentDate, string what, bool applyFloor, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);

        // The floor first: a date before the shop existed is a typo, not a locked period, and
        // saying so plainly is more use than "the books are locked up to nothing".
        if (applyFloor && settings.BooksStartFrom is { } startsFrom && documentDate < startsFrom)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Date"] =
                [
                    $"The books begin on {startsFrom:dd-MM-yyyy}, so this {what} dated "
                    + $"{documentDate:dd-MM-yyyy} is before the shop's records start. "
                    + "Check the year.",
                ],
            });
        }

        if (settings.BooksLockedUpTo is not { } lockedUpTo || documentDate > lockedUpTo)
        {
            return;
        }

        // Not a ForbiddenException: nobody may do this, not even the owner, without first moving the
        // lock. Framing it as a permission problem would send staff to ask the owner to do the same
        // forbidden thing.
        throw new ValidationAppException(new Dictionary<string, string[]>
        {
            ["Date"] =
            [
                $"Books are locked up to {lockedUpTo:dd-MM-yyyy}, so this {what} dated " +
                $"{documentDate:dd-MM-yyyy} cannot be changed. Unlock the period first if it really has to move.",
            ],
        });
    }
}

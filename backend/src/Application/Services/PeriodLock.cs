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

    public async Task GuardAsync(DateOnly documentDate, string what, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);

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

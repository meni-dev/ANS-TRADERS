using Application.Common;
using Application.Common.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace UnitTests;

internal sealed class FakeShopSettingsRepository : IShopSettingsRepository
{
    public ShopSettings Settings { get; } = new();

    public Task<ShopSettings> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class PeriodLockTests
{
    private static (PeriodLock Lock, FakeShopSettingsRepository Settings) Build(DateOnly? lockedUpTo)
    {
        var settings = new FakeShopSettingsRepository();
        settings.Settings.BooksLockedUpTo = lockedUpTo;
        return (new PeriodLock(settings), settings);
    }

    [Fact]
    public async Task Open_books_allow_any_date()
    {
        var (periodLock, _) = Build(null);

        await periodLock.GuardAsync(new DateOnly(2020, 1, 1), "bill", CancellationToken.None);
    }

    [Fact]
    public async Task A_date_after_the_lock_is_allowed()
    {
        var (periodLock, _) = Build(new DateOnly(2026, 7, 31));

        await periodLock.GuardAsync(new DateOnly(2026, 8, 1), "bill", CancellationToken.None);
    }

    /// <summary>
    /// The boundary is the one that matters: a return filed for July covers 31 July itself, so the
    /// lock has to include its own date rather than stopping the day before.
    /// </summary>
    [Fact]
    public async Task The_locked_date_itself_is_refused()
    {
        var (periodLock, _) = Build(new DateOnly(2026, 7, 31));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            periodLock.GuardAsync(new DateOnly(2026, 7, 31), "bill", CancellationToken.None));
    }

    [Fact]
    public async Task A_date_inside_the_locked_period_is_refused()
    {
        var (periodLock, _) = Build(new DateOnly(2026, 7, 31));

        var error = await Assert.ThrowsAsync<ValidationAppException>(() =>
            periodLock.GuardAsync(new DateOnly(2026, 6, 15), "credit note", CancellationToken.None));

        // The message has to name both dates and the thing being attempted, or whoever hits it at
        // the counter cannot tell whether the app is broken or the month is closed.
        var message = Assert.Single(error.Errors["Date"]);
        Assert.Contains("31-07-2026", message);
        Assert.Contains("15-06-2026", message);
        Assert.Contains("credit note", message);
    }

    /// <summary>
    /// The books-start floor is a typo-catcher for new entries. Applied to a cancel it becomes a
    /// trap: a document that got in before the floor existed could then never be undone, and one
    /// purchase dated 2019 was stuck exactly that way — the app refusing the only remedy it offers.
    /// </summary>
    [Fact]
    public async Task A_document_dated_before_the_books_began_can_still_be_cancelled()
    {
        var (periodLock, settings) = Build(null);
        settings.Settings.BooksStartFrom = new DateOnly(2026, 4, 1);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            periodLock.GuardAsync(new DateOnly(2019, 4, 1), "purchase", CancellationToken.None));

        await periodLock.GuardUndoAsync(new DateOnly(2019, 4, 1), "purchase", CancellationToken.None);
    }

    /// <summary>
    /// The lock is a different thing and still holds. A filed month is closed to cancels as much as
    /// to entries; the way back is to move the lock, deliberately.
    /// </summary>
    [Fact]
    public async Task A_filed_month_is_still_closed_to_cancels()
    {
        var (periodLock, _) = Build(new DateOnly(2026, 7, 31));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            periodLock.GuardUndoAsync(new DateOnly(2026, 7, 15), "bill", CancellationToken.None));
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void A_password_verifies_against_its_own_hash()
    {
        var hash = PasswordHasher.Hash("counter-desk-2026");

        Assert.True(PasswordHasher.Verify("counter-desk-2026", hash));
        Assert.False(PasswordHasher.Verify("counter-desk-2027", hash));
    }

    /// <summary>
    /// Two people picking the same password must not produce the same stored value, or one leaked
    /// hash would name everybody who shares it.
    /// </summary>
    [Fact]
    public void The_same_password_hashes_differently_every_time()
    {
        Assert.NotEqual(PasswordHasher.Hash("same-password"), PasswordHasher.Hash("same-password"));
    }

    [Fact]
    public void A_malformed_stored_hash_verifies_to_false_rather_than_throwing()
    {
        Assert.False(PasswordHasher.Verify("anything", "not-a-hash"));
    }

    [Fact]
    public void Generated_passwords_avoid_characters_that_get_misread_aloud()
    {
        for (var i = 0; i < 200; i++)
        {
            Assert.DoesNotContain(PasswordHasher.GenerateTemporary(), c => "O0Il1".Contains(c));
        }
    }
}

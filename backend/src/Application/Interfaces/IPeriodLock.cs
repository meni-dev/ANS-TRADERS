namespace Application.Interfaces;

/// <summary>
/// Refuses to change anything dated inside a period the shop has already filed.
/// <para>
/// GST returns are filed on what the books said on filing day. A bill added to — or cancelled in —
/// a month already filed puts the app and the return permanently out of step, and the mismatch
/// surfaces months later as a notice, when nobody remembers the edit.
/// </para>
/// </summary>
public interface IPeriodLock
{
    /// <summary>
    /// Throws when <paramref name="documentDate"/> falls on or before the locked date.
    /// <paramref name="what"/> names the thing being attempted, so the message can say what was
    /// refused rather than only that something was.
    /// </summary>
    Task GuardAsync(DateOnly documentDate, string what, CancellationToken cancellationToken);

    /// <summary>
    /// The same lock, without the books-start floor — for undoing a document that already exists.
    /// <para>
    /// The floor is a typo-catcher: it stops somebody entering a bill dated 2019 when the shop
    /// opened in 2026. Applied to a cancel it becomes a trap, because a document that got past the
    /// floor before the floor existed can then never be undone — the app refuses the one remedy it
    /// offers, and the wrong figure stays in the books for good. One such purchase, dated 2019,
    /// was stuck exactly this way.
    /// </para>
    /// <para>
    /// The lock itself still applies. A month that has been filed is closed to cancels as much as
    /// to entries; the way back is to move the lock, deliberately.
    /// </para>
    /// </summary>
    Task GuardUndoAsync(DateOnly documentDate, string what, CancellationToken cancellationToken);
}

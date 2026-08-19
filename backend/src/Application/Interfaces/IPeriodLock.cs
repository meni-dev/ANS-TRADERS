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
}

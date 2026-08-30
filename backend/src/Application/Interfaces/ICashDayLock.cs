namespace Application.Interfaces;

/// <summary>
/// A day that has been counted and closed is a fixed point. This refuses anything that would move
/// cash into or out of it afterwards.
/// <para>
/// The books lock (<see cref="IPeriodLock"/>) is a different thing and both apply: that one freezes
/// a filed month, this one freezes a counted day. A shop closes a day long before it files the
/// month, so without this the drawer figure someone signed off on can be changed under them —
/// yesterday's close says the till held ₹4,000, a back-dated cash receipt is entered, and the cash
/// book now says ₹4,500 for a day the owner already counted and agreed.
/// </para>
/// <para>
/// Only cash matters here. A credit sale, a UPI receipt or a bank transfer on a closed day changes
/// nothing about what was in the drawer, so they are left alone.
/// </para>
/// </summary>
public interface ICashDayLock
{
    /// <summary>
    /// Throws when <paramref name="entryDate"/> falls on or before a day that has been closed and
    /// the entry moves cash. <paramref name="what"/> names the thing being entered, so the message
    /// can say which one.
    /// </summary>
    Task GuardAsync(DateOnly entryDate, string what, bool movesCash, CancellationToken cancellationToken);
}

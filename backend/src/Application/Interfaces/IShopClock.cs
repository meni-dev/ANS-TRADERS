namespace Application.Interfaces;

/// <summary>
/// What day it is where the shop is.
/// <para>
/// The server runs on UTC — it did in Docker and it will on Lambda — so <c>DateTime.Today</c> is
/// yesterday in India between midnight and 5:30 in the morning. Anything that decides a calendar day
/// (a bill's date, a day close, whether the books are locked) has to ask the shop's own clock, or a
/// bill written at 1am carries the wrong date and the register it lands in is the wrong one.
/// </para>
/// <para>
/// Instants are a different thing and stay UTC: <c>CreatedAt</c>, an audit row's <c>OccurredAt</c>,
/// a session's expiry. Those are moments, not days, and moments have no timezone problem.
/// </para>
/// </summary>
public interface IShopClock
{
    /// <summary>Today's date in the shop's timezone.</summary>
    DateOnly Today { get; }

    /// <summary>Now, as the shop's wall clock reads it.</summary>
    DateTimeOffset Now { get; }
}

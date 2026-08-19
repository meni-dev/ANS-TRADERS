using Application.Interfaces;

namespace Application.Common;

/// <inheritdoc />
public class ShopClock : IShopClock
{
    /// <summary>
    /// Where the shop is, unless told otherwise. A default belongs here rather than in a required
    /// setting: a missing value would silently mean UTC, which is the exact bug this class removes.
    /// </summary>
    public const string DefaultTimeZone = "Asia/Kolkata";

    private readonly TimeZoneInfo _zone;

    /// <remarks>
    /// Takes the zone itself, not a configuration object. It keeps the clock a thing that can be
    /// constructed in a test with no host around it, and it keeps the decision about what to do with
    /// an unreadable setting where that decision can be logged.
    /// </remarks>
    public ShopClock(TimeZoneInfo zone)
    {
        _zone = zone;
    }

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    /// <summary>
    /// Reads a timezone name, falling back to UTC and reporting that it did.
    /// <para>
    /// Throwing would take the whole app down over a typo in a setting; falling back in silence
    /// would misdate every document for months before anybody noticed. So it does neither.
    /// </para>
    /// </summary>
    public static bool TryResolve(string? id, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(
                string.IsNullOrWhiteSpace(id) ? DefaultTimeZone : id.Trim());
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            zone = TimeZoneInfo.Utc;
            return false;
        }
    }
}

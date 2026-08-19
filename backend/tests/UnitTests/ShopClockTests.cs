using Application.Common;
using Application.Interfaces;

namespace UnitTests;

public class ShopClockTests
{
    private static IShopClock Build(string? zone)
    {
        ShopClock.TryResolve(zone, out var resolved);
        return new ShopClock(resolved);
    }

    /// <summary>
    /// The whole reason this class exists. At 1am in Chennai the server's own clock still reads the
    /// previous day, and a bill written then would be dated — and reported — a day early.
    /// </summary>
    [Fact]
    public void Early_morning_in_the_shop_is_still_the_previous_day_in_UTC()
    {
        // 2026-08-19 20:00 UTC is 2026-08-20 01:30 in Chennai.
        var instant = new DateTimeOffset(2026, 8, 19, 20, 0, 0, TimeSpan.Zero);
        var chennai = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        Assert.Equal(new DateOnly(2026, 8, 19), DateOnly.FromDateTime(instant.UtcDateTime));
        Assert.Equal(
            new DateOnly(2026, 8, 20),
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, chennai).DateTime));
    }

    [Fact]
    public void The_shop_defaults_to_India_when_nothing_is_configured()
    {
        var expected = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));

        Assert.Equal(DateOnly.FromDateTime(expected.DateTime), Build(null).Today);
    }

    [Fact]
    public void A_configured_timezone_is_used()
    {
        var clock = Build("UTC");

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), clock.Today);
    }

    /// <summary>
    /// A name the machine does not know falls back to UTC and says so in the log. Throwing would
    /// take the whole app down over a typo; failing silently would misdate documents for months.
    /// </summary>
    [Fact]
    public void An_unknown_timezone_falls_back_rather_than_throwing()
    {
        var clock = Build("Mars/Olympus_Mons");

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), clock.Today);
    }
}

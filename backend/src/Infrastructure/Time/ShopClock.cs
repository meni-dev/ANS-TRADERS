using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Time;

/// <inheritdoc />
public class ShopClock : IShopClock
{
    /// <summary>
    /// Where the shop is, unless told otherwise. A default is right here rather than a required
    /// setting: a missing value would otherwise silently mean UTC, which is the exact bug this class
    /// exists to remove.
    /// </summary>
    private const string DefaultTimeZone = "Asia/Kolkata";

    private readonly TimeZoneInfo _zone;

    public ShopClock(IConfiguration configuration, ILogger<ShopClock> logger)
    {
        var id = configuration["Shop:TimeZone"] ?? DefaultTimeZone;

        try
        {
            _zone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Falling back to UTC silently would date documents wrongly for months before anybody
            // noticed, so the log says plainly which name was not understood.
            logger.LogError(ex, "Shop:TimeZone '{TimeZone}' was not recognised; falling back to UTC", id);
            _zone = TimeZoneInfo.Utc;
        }
    }

    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zone);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}

using Application.DTOs.Stock;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Three readings of the same shelf: what is not moving, what is no longer worth what it costs, and
/// what is about to run out.
/// <para>
/// None of these is a number the app can hand over as an instruction. Each one is a shortlist with
/// the working shown, so the person who knows the trade can look at it and decide.
/// </para>
/// </summary>
public class StockInsightService : IStockInsightService
{
    /// <summary>
    /// Long enough that a genuinely seasonal part is not condemned, short enough that money stuck
    /// on a shelf is found in the same year it was spent.
    /// </summary>
    private const int DefaultMonthsWithoutSale = 6;

    /// <summary>Three months of selling is enough to see a pattern without a slow week ruining it.</summary>
    private const int DefaultVelocityWindowDays = 90;

    /// <summary>What the shop wants on the shelf, measured in days rather than in pieces.</summary>
    private const int DefaultCoverDays = 45;

    private const decimal DefaultMarginFloorPercent = 15m;

    private readonly IStockRepository _repository;
    private readonly ICurrentUser _currentUser;

    private readonly IShopClock _clock;

    public StockInsightService(IStockRepository repository, ICurrentUser currentUser, IShopClock clock)
    {
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
    }

    /// <summary>
    /// All three reports print buying prices and what the shelf is worth, so all three sit behind
    /// the cost permission. A dead-stock list with the values stripped out would be a list of parts
    /// with nothing to act on.
    /// </summary>
    private void RequireCost(string action) => _currentUser.Require(Permission.CostView, action);

    public async Task<DeadStockReportDto> GetDeadStockAsync(
        int? monthsWithoutSale, CancellationToken cancellationToken)
    {
        RequireCost("see what stock is not moving");

        var months = monthsWithoutSale is > 0 ? monthsWithoutSale.Value : DefaultMonthsWithoutSale;
        var asOf = Today();
        var cutOff = asOf.AddMonths(-months);

        var facts = await _repository.GetShelfFactsAsync(asOf, DefaultVelocityWindowDays, cancellationToken);

        var rows = facts
            // Nothing on the shelf is nothing at risk. A part with no stock that also has no sales
            // is a catalogue entry, not money sitting still.
            .Where(f => f.StockOnHand > 0 && (f.LastSoldOn is null || f.LastSoldOn < cutOff))
            .Select(f => new DeadStockRowDto(
                f.ProductId,
                f.PartNumber,
                f.ItemName,
                f.VehicleBrand,
                f.StockOnHand,
                f.PurchaseRate,
                Money(f.StockOnHand * f.PurchaseRate),
                f.LastSoldOn,
                f.LastSoldOn is { } soldOn ? asOf.DayNumber - soldOn.DayNumber : null))
            // Worst money first. Sorting by how long it has sat would put a ₹40 washer above a
            // ₹40,000 shelf of engine parts.
            .OrderByDescending(r => r.ValueAtCost)
            .ToList();

        var neverSold = rows.Where(r => r.LastSoldOn is null).ToList();

        return new DeadStockReportDto(
            months,
            asOf,
            Money(rows.Sum(r => r.ValueAtCost)),
            neverSold.Count,
            Money(neverSold.Sum(r => r.ValueAtCost)),
            rows);
    }

    public async Task<RateDriftReportDto> GetRateDriftAsync(
        decimal? marginFloorPercent, CancellationToken cancellationToken)
    {
        RequireCost("see margins");

        var floor = marginFloorPercent is >= 0 ? marginFloorPercent.Value : DefaultMarginFloorPercent;
        var facts = await _repository.GetShelfFactsAsync(Today(), DefaultVelocityWindowDays, cancellationToken);

        var rows = facts
            .Where(f => f.LastPurchaseRate is > 0)
            .Select(f =>
            {
                // Margin is measured against the newest buying price, not the catalogue's. The
                // catalogue rate is what the shop last typed in; the bill is what the supplier
                // actually charges, and that is what the next box will cost.
                var cost = f.LastPurchaseRate!.Value;
                decimal? margin = f.SellingRate <= 0
                    ? null
                    : Math.Round(100m * (f.SellingRate - cost) / f.SellingRate, 1, MidpointRounding.AwayFromZero);

                return new RateDriftRowDto(
                    f.ProductId,
                    f.PartNumber,
                    f.ItemName,
                    f.StockOnHand,
                    cost,
                    f.LastPurchasedOn,
                    f.PurchaseRate,
                    f.SellingRate,
                    f.Mrp,
                    margin,
                    f.SellingRate > 0 && f.SellingRate < cost,
                    f.SellingRate <= 0);
            })
            .Where(r => r.SellingBelowCost || r.SellingRateMissing || r.MarginPercent < floor)
            // Selling below cost first — every one of those is a loss the shop is making on purpose
            // without knowing it — then unpriced, then thinnest margin.
            .OrderByDescending(r => r.SellingBelowCost)
            .ThenByDescending(r => r.SellingRateMissing)
            .ThenBy(r => r.MarginPercent ?? decimal.MaxValue)
            .ToList();

        return new RateDriftReportDto(
            floor,
            rows.Count(r => r.SellingBelowCost),
            rows.Count(r => !r.SellingBelowCost && !r.SellingRateMissing),
            rows.Count(r => r.SellingRateMissing),
            rows);
    }

    public async Task<ReorderReportDto> GetReorderAsync(
        int? coverDays, CancellationToken cancellationToken)
    {
        RequireCost("see the buying list");

        var cover = coverDays is > 0 ? coverDays.Value : DefaultCoverDays;
        var asOf = Today();
        var facts = await _repository.GetShelfFactsAsync(asOf, DefaultVelocityWindowDays, cancellationToken);

        var rows = new List<ReorderRowDto>();

        foreach (var f in facts)
        {
            if (!f.IsActive)
            {
                continue;
            }

            var velocity = Math.Round(f.QuantitySoldInWindow / DefaultVelocityWindowDays, 4, MidpointRounding.AwayFromZero);
            var target = Math.Ceiling(velocity * cover);

            // Two reasons to be on this list, and they are different questions. Velocity says the
            // shelf will empty; the reorder level is the shop's own standing instruction for a part
            // that must be there whether or not it has sold lately.
            var short_ = Math.Max(target - f.StockOnHand, f.ReorderLevel - f.StockOnHand);

            if (short_ <= 0)
            {
                continue;
            }

            var rate = f.LastPurchaseRate ?? f.PurchaseRate;

            rows.Add(new ReorderRowDto(
                f.ProductId,
                f.PartNumber,
                f.ItemName,
                f.StockOnHand,
                f.ReorderLevel,
                velocity,
                // A part that is not moving has no date at which it runs out. Reporting a huge
                // number of days would be arithmetic pretending to be knowledge.
                velocity > 0 ? (int)Math.Floor(f.StockOnHand / velocity) : null,
                short_,
                rate,
                Money(short_ * rate)));
        }

        return new ReorderReportDto(
            DefaultVelocityWindowDays,
            cover,
            Money(rows.Sum(r => r.SuggestedValue)),
            rows.Count(r => r.StockOnHand <= 0),
            // Emptiest shelf first: nothing on hand and still selling is the one that costs a sale
            // today, not next month.
            rows.OrderBy(r => r.DaysOfCover ?? int.MaxValue).ThenByDescending(r => r.SuggestedValue).ToList());
    }

    private DateOnly Today() => _clock.Today;

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

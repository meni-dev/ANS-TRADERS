using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace UnitTests;

/// <summary>Hands back a fixed set of shelf facts, so the reports can be reasoned about exactly.</summary>
internal sealed class FakeShelfRepository : IStockRepository
{
    public List<ProductShelfFacts> Facts { get; } = [];

    public int VelocityWindowSeen { get; private set; }

    public Task<IReadOnlyList<ProductShelfFacts>> GetShelfFactsAsync(
        DateOnly asOf, int velocityWindowDays, CancellationToken cancellationToken)
    {
        VelocityWindowSeen = velocityWindowDays;
        return Task.FromResult<IReadOnlyList<ProductShelfFacts>>(Facts);
    }

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchStockAsync(
        string? search, bool? lowOnly, bool? activeOnly, int page, int pageSize,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<(int TotalItems, int LowStockCount, int OutOfStockCount, decimal TotalStockValue)>
        GetStockSummaryAsync(string? search, bool? lowOnly, bool? activeOnly, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> SearchMovementsAsync(
        string? search, Guid? productId, StockMovementType? movementType, DateOnly? fromDate,
        DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<(StockAdjustmentReason Reason, decimal Quantity, decimal Value)>>
        GetAdjustmentsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Holds every permission. These tests are about the arithmetic of the three reports, not about who
/// is allowed to run them — <see cref="PermissionGuardTests"/> covers that.
/// </summary>
internal sealed class AllowAllCurrentUser : ICurrentUser
{
    public Guid? UserId => Guid.Empty;

    public string Name => "test";

    public string RoleName => "test";

    public IReadOnlySet<Permission> Permissions { get; } = Enum.GetValues<Permission>().ToHashSet();

    public bool Has(Permission permission) => true;

    public void Require(Permission permission, string action)
    {
    }
}

/// <summary>
/// A clock that does not move, so a test written today still means the same thing next March.
/// </summary>
internal sealed class FixedClock : IShopClock
{
    private readonly DateOnly _today;

    public FixedClock(DateOnly today)
    {
        _today = today;
    }

    public DateOnly Today => _today;

    public DateTimeOffset Now => new(_today.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(5.5));
}

public class StockInsightTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private static ProductShelfFacts Part(
        string partNumber,
        decimal stock = 10m,
        decimal purchaseRate = 100m,
        decimal sellingRate = 150m,
        decimal reorderLevel = 0m,
        bool isActive = true,
        DateOnly? lastSoldOn = null,
        decimal soldInWindow = 0m,
        decimal? lastPurchaseRate = 100m) =>
        new(
            Guid.NewGuid(), partNumber, partNumber, "Honda", stock, purchaseRate, sellingRate,
            Mrp: 0m, reorderLevel, isActive, lastSoldOn, soldInWindow,
            LastPurchasedOn: Today.AddDays(-10), lastPurchaseRate);

    private static (StockInsightService Service, FakeShelfRepository Repository) Build(
        params ProductShelfFacts[] facts)
    {
        var repository = new FakeShelfRepository();
        repository.Facts.AddRange(facts);
        return (new StockInsightService(repository, new AllowAllCurrentUser(), new FixedClock(Today)), repository);
    }

    // ------------------------------------------------------------ Dead stock

    [Fact]
    public async Task A_part_with_no_stock_is_not_dead_money()
    {
        // Nothing on the shelf is nothing at risk, however long ago it last sold.
        var (service, _) = Build(Part("EMPTY", stock: 0m, lastSoldOn: Today.AddYears(-3)));

        var report = await service.GetDeadStockAsync(6, CancellationToken.None);

        Assert.Empty(report.Rows);
        Assert.Equal(0m, report.TotalValue);
    }

    [Fact]
    public async Task A_part_never_sold_is_counted_separately_from_one_merely_idle()
    {
        var (service, _) = Build(
            Part("NEVER", stock: 2m, purchaseRate: 500m, lastSoldOn: null),
            Part("IDLE", stock: 1m, purchaseRate: 100m, lastSoldOn: Today.AddMonths(-9)));

        var report = await service.GetDeadStockAsync(6, CancellationToken.None);

        Assert.Equal(2, report.Rows.Count);
        Assert.Equal(1, report.NeverSoldCount);
        Assert.Equal(1000m, report.NeverSoldValue);
        Assert.Equal(1100m, report.TotalValue);

        // Worst money first, not longest idle — a ₹40 washer must not sit above a ₹1,000 shelf.
        Assert.Equal("NEVER", report.Rows[0].PartNumber);
    }

    [Fact]
    public async Task A_part_sold_inside_the_window_is_left_alone()
    {
        var (service, _) = Build(Part("MOVING", stock: 5m, lastSoldOn: Today.AddMonths(-2)));

        var report = await service.GetDeadStockAsync(6, CancellationToken.None);

        Assert.Empty(report.Rows);
    }

    // ------------------------------------------------------------ Rate drift

    [Fact]
    public async Task Margin_is_measured_against_the_newest_bill_not_the_catalogue()
    {
        // The catalogue still says 100, but the supplier now charges 140 — so the real margin on
        // a 150 sale is 6.7%, not 33%, and this part belongs on the list.
        var (service, _) = Build(Part("RISEN", purchaseRate: 100m, sellingRate: 150m, lastPurchaseRate: 140m));

        var report = await service.GetRateDriftAsync(15m, CancellationToken.None);

        var row = Assert.Single(report.Rows);
        Assert.Equal(6.7m, row.MarginPercent);
        Assert.False(row.SellingBelowCost);
    }

    [Fact]
    public async Task Selling_below_the_newest_cost_is_flagged_and_listed_first()
    {
        var (service, _) = Build(
            Part("THIN", sellingRate: 110m, lastPurchaseRate: 100m),
            Part("LOSS", sellingRate: 90m, lastPurchaseRate: 100m));

        var report = await service.GetRateDriftAsync(15m, CancellationToken.None);

        Assert.Equal("LOSS", report.Rows[0].PartNumber);
        Assert.True(report.Rows[0].SellingBelowCost);
        Assert.Equal(1, report.BelowCostCount);
        Assert.Equal(1, report.ThinMarginCount);
    }

    /// <summary>
    /// An unpriced part has no margin — that is an unanswered question, not a margin of zero.
    /// Reporting zero would bury the genuinely thin margins under stock nobody has priced.
    /// </summary>
    [Fact]
    public async Task A_part_with_no_selling_price_has_no_margin_rather_than_a_margin_of_zero()
    {
        var (service, _) = Build(Part("UNPRICED", sellingRate: 0m, lastPurchaseRate: 50m));

        var report = await service.GetRateDriftAsync(15m, CancellationToken.None);

        var row = Assert.Single(report.Rows);
        Assert.Null(row.MarginPercent);
        Assert.True(row.SellingRateMissing);
        Assert.False(row.SellingBelowCost);
        Assert.Equal(1, report.UnpricedCount);
        Assert.Equal(0, report.ThinMarginCount);
    }

    [Fact]
    public async Task A_part_never_bought_through_the_app_has_no_cost_to_compare_against()
    {
        var (service, _) = Build(Part("OPENING", lastPurchaseRate: null));

        var report = await service.GetRateDriftAsync(15m, CancellationToken.None);

        Assert.Empty(report.Rows);
    }

    // -------------------------------------------------------------- Reorder

    [Fact]
    public async Task What_to_buy_comes_from_what_actually_sold()
    {
        // 90 sold over the 90-day window is one a day; 45 days of cover means 45 on the shelf, and
        // there are 10, so 35 are short.
        var (service, repository) = Build(Part("FAST", stock: 10m, soldInWindow: 90m));

        var report = await service.GetReorderAsync(45, CancellationToken.None);

        Assert.Equal(90, repository.VelocityWindowSeen);
        var row = Assert.Single(report.Rows);
        Assert.Equal(1m, row.DailyVelocity);
        Assert.Equal(10, row.DaysOfCover);
        Assert.Equal(35m, row.SuggestedQuantity);
    }

    [Fact]
    public async Task A_part_that_is_not_moving_has_no_date_at_which_it_runs_out()
    {
        // On the list only because it is under its own reorder level, and reported with no cover
        // figure rather than a fabricated one.
        var (service, _) = Build(Part("STILL", stock: 2m, reorderLevel: 5m, soldInWindow: 0m));

        var report = await service.GetReorderAsync(45, CancellationToken.None);

        var row = Assert.Single(report.Rows);
        Assert.Null(row.DaysOfCover);
        Assert.Equal(0m, row.DailyVelocity);
        Assert.Equal(3m, row.SuggestedQuantity);
    }

    [Fact]
    public async Task A_shelf_with_enough_on_it_is_not_on_the_buying_list()
    {
        var (service, _) = Build(Part("STOCKED", stock: 100m, soldInWindow: 90m, reorderLevel: 5m));

        var report = await service.GetReorderAsync(45, CancellationToken.None);

        Assert.Empty(report.Rows);
    }

    [Fact]
    public async Task An_inactive_part_is_never_suggested_for_reorder()
    {
        var (service, _) = Build(Part("RETIRED", stock: 0m, reorderLevel: 10m, isActive: false));

        var report = await service.GetReorderAsync(45, CancellationToken.None);

        Assert.Empty(report.Rows);
    }
}

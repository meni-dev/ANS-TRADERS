namespace Application.DTOs.Stock;

/// <summary>
/// A part that is sitting on the shelf rather than moving.
/// <para>
/// This is the biggest hole in a spare parts shop's money: a part bought once, stocked for a vehicle
/// nobody rides any more, and never counted as a loss because it is technically still an asset.
/// </para>
/// </summary>
public record DeadStockRowDto(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string? VehicleBrand,
    decimal StockOnHand,
    decimal PurchaseRate,
    /// <summary>What is tied up in this part, at what it cost.</summary>
    decimal ValueAtCost,
    /// <summary>Null when it has never been sold at all — a different, worse case than "not lately".</summary>
    DateOnly? LastSoldOn,
    int? DaysSinceLastSale);

public record DeadStockReportDto(
    /// <summary>How long a part has to sit still before it counts as dead.</summary>
    int MonthsWithoutSale,
    DateOnly AsOf,
    decimal TotalValue,
    int NeverSoldCount,
    decimal NeverSoldValue,
    IReadOnlyList<DeadStockRowDto> Rows);

/// <summary>
/// A part whose buying price has moved and whose selling price has not followed.
/// </summary>
public record RateDriftRowDto(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    decimal StockOnHand,
    /// <summary>The rate on the newest purchase bill for this part.</summary>
    decimal LastPurchaseRate,
    DateOnly? LastPurchasedOn,
    /// <summary>The rate the catalogue is currently costing it at.</summary>
    decimal CataloguePurchaseRate,
    decimal SellingRate,
    decimal Mrp,
    /// <summary>
    /// Margin against the newest buying price, which is what the next box will cost. Null when the
    /// part has no selling rate at all — that is not a margin of zero, it is an unanswered question,
    /// and showing it as zero would bury the genuinely thin margins under unpriced stock.
    /// </summary>
    decimal? MarginPercent,
    /// <summary>True when the shop is selling this part for less than it now pays for it.</summary>
    bool SellingBelowCost,
    /// <summary>Nobody has set a selling price. It cannot be billed correctly until somebody does.</summary>
    bool SellingRateMissing);

public record RateDriftReportDto(
    decimal MarginFloorPercent,
    int BelowCostCount,
    int ThinMarginCount,
    int UnpricedCount,
    IReadOnlyList<RateDriftRowDto> Rows);

/// <summary>
/// What to buy, ordered by how soon the shelf runs out rather than by a fixed level.
/// </summary>
public record ReorderRowDto(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    decimal StockOnHand,
    decimal ReorderLevel,
    /// <summary>Sold per day, measured over the window. Zero for anything that has not moved.</summary>
    decimal DailyVelocity,
    /// <summary>Null when nothing is moving — there is no date at which a part nobody buys runs out.</summary>
    int? DaysOfCover,
    decimal SuggestedQuantity,
    decimal LastPurchaseRate,
    decimal SuggestedValue);

public record ReorderReportDto(
    int WindowDays,
    int CoverDays,
    decimal TotalSuggestedValue,
    int OutOfStockCount,
    IReadOnlyList<ReorderRowDto> Rows);

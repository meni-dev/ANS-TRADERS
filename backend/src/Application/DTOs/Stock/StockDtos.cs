namespace Application.DTOs.Stock;

/// <summary>
/// A product as the stock screen sees it — identity plus quantities, no pricing detail.
/// <paramref name="StockValue"/> is the stock held at the item's purchase rate: what the shelf is
/// worth to the shop.
/// </summary>
public record ProductStockDto(
    Guid Id,
    string PartNumber,
    string ItemName,
    string? VehicleBrand,
    string? VehicleModel,
    string Uqc,
    decimal OpeningStock,
    decimal StockOnHand,
    decimal ReorderLevel,
    decimal StockValue,
    bool IsActive);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string MovementType,
    decimal Quantity,
    decimal BalanceAfter,
    /// <summary>The day the goods moved, from the document that moved them. What the screen shows.</summary>
    DateOnly MovementDate,
    /// <summary>When the row was written. Audit only — see <see cref="MovementDate"/>.</summary>
    DateTimeOffset MovedAt,
    Guid? ReferenceId,
    string? ReferenceNumber,
    string? Notes);

public record StockListQuery(string? Search, bool? LowOnly, bool? ActiveOnly, int Page = 1, int PageSize = 20);

public record StockMovementListQuery(
    string? Search,
    Guid? ProductId,
    string? MovementType,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// A physical count, not a delta. The counter types what is actually on the shelf and the service
/// works out the correction — asking for "+3" or "−3" is how a recount turns into a second error.
/// </summary>
/// <summary>
/// <paramref name="Reason"/> is the code the loss report counts; <paramref name="Notes"/> is the
/// sentence that explains this one instance.
/// </summary>
public record AdjustStockRequest(
    Guid ProductId, decimal CountedQuantity, string Reason, string? Notes = null);

public record StockLossRowDto(
    string Reason,
    string Label,
    decimal Quantity,
    /// <summary>Valued at the product's purchase rate — what the loss actually cost the shop.</summary>
    decimal Value,
    int Movements);

/// <summary>
/// What walked off the shelf without being sold, and why. Only losses: a counting error that found
/// stock is not a loss, and mixing the two makes the total meaningless.
/// </summary>
public record StockLossReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalValue,
    IReadOnlyList<StockLossRowDto> ByReason);

/// <summary>Headline figures for the stock screen, across everything the current filter matches.</summary>
public record StockSummaryDto(
    int TotalItems,
    int LowStockCount,
    int OutOfStockCount,
    decimal TotalStockValue);

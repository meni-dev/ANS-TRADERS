using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IStockRepository
{
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken);

    /// <summary>
    /// What the shelf held at the end of <paramref name="onDate"/>, from the movements themselves.
    /// <para>
    /// <see cref="Domain.Entities.Product.StockOnHand"/> answers for today and nothing else, so a
    /// back-dated document has to be checked against the day it claims to have happened on.
    /// </para>
    /// </summary>
    Task<decimal> GetBalanceOnAsync(Guid productId, DateOnly onDate, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchStockAsync(
        string? search, bool? lowOnly, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Totals across everything the same filter matches, not just the page on screen — the counts on
    /// the stock screen are about the whole shelf, so they cannot be derived from the current page.
    /// </summary>
    Task<(int TotalItems, int LowStockCount, int OutOfStockCount, decimal TotalStockValue)> GetStockSummaryAsync(
        string? search, bool? lowOnly, bool? activeOnly, CancellationToken cancellationToken);

    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> SearchMovementsAsync(
        string? search,
        Guid? productId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adjustment rows over a range with the value they moved, so the service can group them by
    /// reason. Valued at the product's purchase rate — a loss costs what the goods cost.
    /// </summary>
    Task<IReadOnlyList<(Domain.Enums.StockAdjustmentReason Reason, decimal Quantity, decimal Value)>>
        GetAdjustmentsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>
    /// One row per product with everything the three shelf reports need — what is on hand, when it
    /// last moved, how fast, and what it last cost.
    /// <para>
    /// Gathered once rather than per report, because dead stock, rate drift and reorder are three
    /// readings of the same facts and would otherwise ask the database the same questions three
    /// times over.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<ProductShelfFacts>> GetShelfFactsAsync(
        DateOnly asOf, int velocityWindowDays, CancellationToken cancellationToken);
}

/// <param name="LastSoldOn">Null when the part has never been sold.</param>
/// <param name="LastPurchaseRate">Null when it has never been bought through the app.</param>
public record ProductShelfFacts(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string? VehicleBrand,
    decimal StockOnHand,
    decimal PurchaseRate,
    decimal SellingRate,
    decimal Mrp,
    decimal ReorderLevel,
    bool IsActive,
    DateOnly? LastSoldOn,
    decimal QuantitySoldInWindow,
    DateOnly? LastPurchasedOn,
    decimal? LastPurchaseRate);

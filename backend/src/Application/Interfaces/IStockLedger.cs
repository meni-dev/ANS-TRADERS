using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// The only way stock is allowed to change. Purchases, sales, cancellations and manual corrections
/// all go through here so that <see cref="Product.StockOnHand"/> and the ledger can never drift
/// apart — a service that set the quantity directly would leave an unexplained number behind.
/// </summary>
public interface IStockLedger
{
    /// <summary>
    /// Applies a signed quantity change to <paramref name="product"/> and appends the matching
    /// ledger row. Nothing is persisted: the caller saves, so the movement commits in the same
    /// transaction as the document that caused it.
    /// </summary>
    Task RecordAsync(
        Product product,
        decimal signedQuantity,
        StockMovementType movementType,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken,
        /// <summary>Only meaningful on an adjustment — see <see cref="StockMovement.AdjustmentReason"/>.</summary>
        StockAdjustmentReason? adjustmentReason = null);

    /// <summary>
    /// Throws when the product cannot cover <paramref name="quantity"/>. Called before a line that
    /// takes stock off the shelf is written, so the document is rejected whole rather than
    /// half-applied.
    /// <para>
    /// <paramref name="action"/> is the verb the message uses. Two things take stock out — billing a
    /// customer and sending goods back to a supplier — and telling somebody raising a return that
    /// the shop "cannot bill" it sends them looking in the wrong place.
    /// </para>
    /// </summary>
    void EnsureAvailable(Product product, decimal quantity, string action = "bill");
}

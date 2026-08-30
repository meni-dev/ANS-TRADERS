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
        /// <summary>The document's own date — see <see cref="StockMovement.MovementDate"/>.</summary>
        DateOnly movementDate,
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

    /// <summary>
    /// Throws when undoing a document would take more off the shelf than is still on it.
    /// <para>
    /// Cancelling a purchase, or cancelling a credit note, both put a document into reverse and
    /// take stock back out. If the goods have since been sold on there is nothing left to take, and
    /// the reversal would leave a negative quantity on the shelf — a figure that is not wrong by a
    /// rounding but wrong in kind, and that then values the stock at a negative number.
    /// </para>
    /// <para>
    /// It refuses rather than clamping, because the arithmetic is telling the truth: goods that were
    /// sold really did arrive, so the document being cancelled describes something that happened.
    /// <paramref name="undoing"/> names the document so the message can say which one.
    /// </para>
    /// </summary>
    void EnsureReversible(Product product, decimal quantity, string undoing, string remedy);

    /// <summary>
    /// Throws when the shelf could not have covered <paramref name="quantity"/> on
    /// <paramref name="onDate"/>.
    /// <para>
    /// <see cref="EnsureAvailable"/> asks about today, which is the right question for a document
    /// dated today and the wrong one for a back-dated bill: back-date it to a week the shelf was
    /// empty and today's stock waves it through, leaving the books showing goods sold before they
    /// arrived. Replaying this shop's own movements in document-date order found six such lines.
    /// </para>
    /// <para>
    /// A document dated today takes the cheap path — the common case does not pay for the query.
    /// </para>
    /// </summary>
    Task EnsureAvailableOnAsync(
        Product product, decimal quantity, DateOnly onDate, string action, CancellationToken cancellationToken);
}

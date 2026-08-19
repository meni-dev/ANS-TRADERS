using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One line of the stock ledger: an append-only record of every quantity change, with the document
/// that caused it. <see cref="Product.StockOnHand"/> is the running total of these rows — kept on
/// the product so the billing screen can read stock without summing the ledger, and reconcilable
/// against it at any time.
/// </summary>
public class StockMovement : Entity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Snapshot, so the ledger still reads correctly after a part is renamed.</summary>
    public string PartNumber { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Signed: positive brings stock in, negative takes it out. One column rather than a quantity
    /// plus a direction flag, so the balance is a plain SUM and cannot disagree with itself.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>Stock on hand immediately after this movement. Makes the ledger readable on its own.</summary>
    public decimal BalanceAfter { get; set; }

    public DateTimeOffset MovedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The purchase or invoice that caused the movement. Null for opening and adjustments.</summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>Human-readable document number, e.g. <c>INV/2026-27/0007</c>.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Set only on <see cref="StockMovementType.Adjustment"/> rows. Everything else already carries
    /// its reason in the document behind it, so a code there would be noise.
    /// </summary>
    public StockAdjustmentReason? AdjustmentReason { get; set; }

    public string? Notes { get; set; }
}

namespace Domain.Enums;

/// <summary>
/// Why stock moved. Every change to a product's quantity is one of these, so the ledger can always
/// answer "where did this number come from" rather than just holding a running total.
/// </summary>
public enum StockMovementType
{
    /// <summary>What was on the shelf when the item started being tracked.</summary>
    Opening = 0,

    Purchase = 1,
    Sale = 2,

    /// <summary>Puts back stock a cancelled supplier bill had brought in.</summary>
    PurchaseCancelled = 3,

    /// <summary>Returns stock to the shelf when an invoice is cancelled.</summary>
    SaleCancelled = 4,

    /// <summary>Manual correction — a physical count, breakage, or a mis-keyed entry.</summary>
    Adjustment = 5,

    /// <summary>
    /// A customer brought part of a sale back, against a credit note.
    /// <para>
    /// Deliberately not <see cref="SaleCancelled"/>: that says the sale never happened, while this
    /// says it happened and some of it came back. And deliberately not <see cref="Adjustment"/>,
    /// which the audit panel counts as stock that moved with no document behind it — a return has a
    /// document, and filing it as an adjustment would put a papered movement on the auditor's
    /// exception list every single time.
    /// </para>
    /// </summary>
    SalesReturn = 6,

    /// <summary>Puts the goods back out when a credit note is cancelled.</summary>
    SalesReturnCancelled = 7,

    /// <summary>Goods sent back to a supplier, against a debit note. Takes stock off the shelf.</summary>
    PurchaseReturn = 8,

    PurchaseReturnCancelled = 9,
}

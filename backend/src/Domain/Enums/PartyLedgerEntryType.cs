namespace Domain.Enums;

/// <summary>
/// Why a party's balance moved. The mirror of <see cref="StockMovementType"/> on the money side, and
/// like it, cancellations and reversals get their own types rather than editing the original row —
/// a statement has to show what happened, not the tidied-up ending.
/// </summary>
public enum PartyLedgerEntryType
{
    /// <summary>Carried-in balance from before the shop started using this app. No document behind it.</summary>
    Opening = 0,

    /// <summary>A sale was billed to this customer.</summary>
    Invoice = 1,

    InvoiceCancelled = 2,

    /// <summary>A supplier billed the shop.</summary>
    PurchaseBill = 3,

    PurchaseCancelled = 4,

    /// <summary>Money in from a customer.</summary>
    PaymentReceived = 5,

    /// <summary>Money out to a supplier.</summary>
    PaymentMade = 6,

    /// <summary>Keyed in error — the money never arrived. Dated the original day.</summary>
    PaymentCancelled = 7,

    /// <summary>
    /// It genuinely arrived and then failed. Dated the day the bank returned it, and kept visible
    /// forever — it is the evidence for whether to take this party's cheque again.
    /// </summary>
    ChequeBounced = 8,

    /// <summary>Bank charge recovered from the party. Never an invoice — it is not a taxable supply.</summary>
    ChequeBounceCharge = 9,

    /// <summary>Manual correction — a write-off, a rounding difference settled by hand.</summary>
    Adjustment = 10,

    /// <summary>Goods came back from a customer — credits their account by the note's full value.</summary>
    CreditNote = 11,

    CreditNoteCancelled = 12,

    /// <summary>Goods went back to a supplier — reduces what the shop owes them.</summary>
    DebitNote = 13,

    DebitNoteCancelled = 14,

    /// <summary>
    /// Money handed back to a customer. Distinct from <see cref="PaymentMade"/>, which reads as
    /// "we paid a supplier" and would be actively confusing on a customer's statement.
    /// </summary>
    RefundPaid = 15,
}

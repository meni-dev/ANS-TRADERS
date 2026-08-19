namespace Domain.Enums;

/// <summary>
/// A purchase is recorded only once the goods and the supplier's bill are in hand, so there is no
/// draft state to model. Cancellation keeps the row for the audit trail rather than deleting it —
/// a GST document that existed must stay traceable even after it is voided.
/// </summary>
public enum PurchaseStatus
{
    Received = 0,
    Cancelled = 1,
}

/// <summary>
/// Mirrors <see cref="PurchaseStatus"/> on the sales side. An issued invoice is immutable: once a
/// number has gone to a customer it can only be cancelled, never edited.
/// </summary>
public enum InvoiceStatus
{
    Issued = 0,
    Cancelled = 1,
}

/// <summary>
/// A credit note mirrors an invoice exactly: once handed to a customer it can only be cancelled.
/// <para>
/// Note there is deliberately no <c>Returned</c> member on <see cref="InvoiceStatus"/>. How much of
/// an invoice has come back is fully derivable from its <c>CreditAppliedAmount</c> and its lines'
/// <c>ReturnedQuantity</c>; a status would be a second source of truth sitting next to those, free
/// to drift away from them.
/// </para>
/// </summary>
public enum CreditNoteStatus
{
    Issued = 0,
    Cancelled = 1,
}

/// <summary>Mirrors <see cref="CreditNoteStatus"/> on the purchase side.</summary>
public enum DebitNoteStatus
{
    Issued = 0,
    Cancelled = 1,
}

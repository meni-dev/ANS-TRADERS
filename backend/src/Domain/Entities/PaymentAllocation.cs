using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// One payment settling one document. A receipt can cover several bills, and a bill can be settled
/// by several receipts, so this is the join that carries the amount.
/// <para>
/// Rows are never deleted. Releasing an allocation flags it, so a statement can still show that the
/// money was once applied here.
/// </para>
/// </summary>
public class PaymentAllocation : Entity
{
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Loaded when a document is cancelled: releasing the allocation has to hand the money back to
    /// its payment as an advance, which means the payment's own totals move too.
    /// </summary>
    public Payment? Payment { get; set; }

    /// <summary>
    /// Exactly one of this, <see cref="PurchaseId"/>, <see cref="CreditNoteId"/> and
    /// <see cref="DebitNoteId"/> is set — a database check enforces it.
    /// </summary>
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    /// <summary>
    /// Set when this is cash refunded against a credit note. Without it a refund would be a
    /// floating unallocated payment, and the unallocated filter would offer it back at the counter
    /// as if it were still money the customer could spend.
    /// </summary>
    public Guid? CreditNoteId { get; set; }
    public CreditNote? CreditNote { get; set; }

    public Guid? DebitNoteId { get; set; }
    public DebitNote? DebitNote { get; set; }

    /// <summary>Snapshot, so the allocation list reads without a join.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Snapshot too — lets the picker show ageing without reaching for the document.</summary>
    public DateOnly DocumentDate { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// Not the payment's date: money received today can be applied to a bill raised next week, and
    /// an advance can be spent months later.
    /// </summary>
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Set when the money is taken back off this document — the payment was cancelled, its cheque
    /// bounced, or the document was cancelled and the money became an advance. A bool rather than a
    /// status enum: a released allocation has exactly one reason and no further states.
    /// </summary>
    public bool IsReversed { get; set; }
}

using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Money in or out. One entity for both directions rather than two, because the cash book is one
/// book — "what came in and went out today" must not be a union of two identically-shaped queries
/// forever.
/// <para>
/// Party details are snapshotted like every other document here: a receipt already handed over must
/// keep reading as it was printed.
/// </para>
/// </summary>
public class Payment : AuditableEntity
{
    /// <summary>
    /// <c>RCT/2026-27/0001</c> or <c>PMT/2026-27/0001</c>.
    /// <para>
    /// Null for a counter payment. A tender collected against an invoice already has a document the
    /// customer can hold — the invoice itself — and minting a second number for one event is what
    /// later produces two pieces of paper for one transaction. It also keeps the standalone receipt
    /// series genuinely gapless, which the audit check depends on.
    /// </para>
    /// </summary>
    public string? ReceiptNumber { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Position within <see cref="FinancialYear"/>. Null alongside a null receipt number.</summary>
    public int? Sequence { get; set; }

    public PaymentDirection Direction { get; set; }

    /// <summary>
    /// The date the money is <em>effective</em>. For a post-dated cheque this is the cheque's own
    /// date, not the day it was handed over — see <see cref="ChequeDetail.ReceivedOn"/>. Collections
    /// for a day filter on this.
    /// </summary>
    public DateOnly PaymentDate { get; set; }

    /// <summary>Null for a walk-in, exactly as <see cref="Invoice.CustomerId"/> is.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Snapshot. A walk-in has a name and no row.</summary>
    public string PartyName { get; set; } = string.Empty;

    /// <summary>Always positive. The sign lives in <see cref="Direction"/>.</summary>
    public decimal Amount { get; set; }

    /// <summary>Sum of the allocations not reversed. Denormalised so the open-advance list is a plain query.</summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary><see cref="Amount"/> less <see cref="AllocatedAmount"/> — money on account, not yet against a bill.</summary>
    public decimal UnallocatedAmount { get; set; }

    public PaymentMode Mode { get; set; }

    /// <summary>UPI reference, NEFT UTR, card slip number. Cheque numbers live on the cheque row.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Posted;

    /// <summary>
    /// True when this was collected as part of raising a document rather than walked in with.
    /// <para>
    /// Without it, cancelling an invoice cannot tell a tender taken across the counter — which must
    /// be reversed with the bill — from a receipt the customer brought on Thursday, which must
    /// survive as an advance.
    /// </para>
    /// </summary>
    public bool IsCounterPayment { get; set; }

    /// <summary>Present only when <see cref="Mode"/> is <c>Cheque</c>.</summary>
    public ChequeDetail? Cheque { get; set; }

    public List<PaymentAllocation> Allocations { get; set; } = [];
}

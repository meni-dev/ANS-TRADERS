using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Goods the shop sent back to a supplier, against a bill already received. The mirror of
/// <see cref="CreditNote"/>, and it reduces what the shop owes rather than what it is owed.
/// <para>
/// One real asymmetry with the sales side: here the stock goes <b>out</b>, so a debit note has to
/// check the goods are still on the shelf — the same check billing makes. You cannot send back what
/// you have already sold on.
/// </para>
/// <para>
/// Under GST the supplier will normally issue their own credit note for the same goods. Matching the
/// two is GSTR-2B work and is not modelled here; this document is the shop's own record.
/// </para>
/// </summary>
public class DebitNote : AuditableEntity
{
    /// <summary>Running number, e.g. <c>DBN/2026-27/0001</c>.</summary>
    public string DebitNoteNumber { get; set; } = string.Empty;

    /// <summary>From <see cref="NoteDate"/>, not the purchase — see the note on <see cref="CreditNote"/>.</summary>
    public string FinancialYear { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public DateOnly NoteDate { get; set; }

    public Guid PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public string PurchaseNumber { get; set; } = string.Empty;
    public DateOnly PurchaseDate { get; set; }

    /// <summary>Required, unlike a credit note's customer: every purchase has a supplier on file.</summary>
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierGstin { get; set; }
    public string? SupplierStateCode { get; set; }

    /// <summary>Copied from the purchase — see the note on <see cref="CreditNote.IsInterState"/>.</summary>
    public bool IsInterState { get; set; }

    public int ItemCount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }

    /// <summary>See <see cref="CreditNote.AppliedToInvoiceAmount"/> — same rule, against the bill.</summary>
    public decimal AppliedToPurchaseAmount { get; set; }

    /// <summary>Cash the supplier has already given back against this note.</summary>
    public decimal RefundedAmount { get; set; }

    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Issued;

    public List<DebitNoteItem> Items { get; set; } = [];
}

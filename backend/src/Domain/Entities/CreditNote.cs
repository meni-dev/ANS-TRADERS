using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Goods a customer brought back, against an invoice already issued. A separate document, never an
/// edit of the bill: under CGST Act Sec 34 a return is credited by a fresh note, and GSTR-1 reports
/// it in its own table alongside the original invoice number.
/// <para>
/// Shaped field for field like <see cref="Invoice"/> so the two share one calculator and one printed
/// layout, exactly as <see cref="Purchase"/> already does.
/// </para>
/// </summary>
public class CreditNote : AuditableEntity
{
    /// <summary>Running number, e.g. <c>CRN/2026-27/0001</c>. Its own series — see below.</summary>
    public string CreditNoteNumber { get; set; } = string.Empty;

    /// <summary>
    /// Taken from <see cref="NoteDate"/>, <b>not</b> from the invoice. A March sale returned in April
    /// belongs to the new year's series, which is both what GST expects and what keeps the per-year
    /// gap check meaningful.
    /// </summary>
    public string FinancialYear { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public DateOnly NoteDate { get; set; }

    /// <summary>The bill being credited. Required — a credit note with nothing behind it is not one.</summary>
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    /// <summary>Snapshots: GSTR-1 carries the original document's number and date on the same row.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerGstin { get; set; }
    public string? CustomerStateCode { get; set; }

    /// <summary>
    /// Copied from the invoice, never recomputed from today's state codes. If the customer's state
    /// was corrected after the sale, this note still has to reverse the tax that was actually
    /// charged — recomputing would credit IGST against a CGST+SGST bill.
    /// </summary>
    public bool IsInterState { get; set; }

    public int ItemCount { get; set; }

    /// <summary>Why the goods came back. Rule 53(1A)(g) requires it on the printed note.</summary>
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

    /// <summary>
    /// How much of this note went to reducing the original bill, capped at what that bill still had
    /// outstanding. The remainder — <c>GrandTotal</c> less this — is a credit on the customer's
    /// account, to spend on a later bill or take back in cash.
    /// <para>
    /// Capping is what keeps <see cref="Invoice.BalanceDue"/> from going negative. A negative balance
    /// would be hidden by every <c>BalanceDue &gt; 0</c> query and would double-count against the
    /// advance the party ledger is already showing.
    /// </para>
    /// </summary>
    public decimal AppliedToInvoiceAmount { get; set; }

    /// <summary>Cash already handed back against this note. Backed by payment allocation rows.</summary>
    public decimal RefundedAmount { get; set; }

    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Issued;

    public List<CreditNoteItem> Items { get; set; } = [];
}

using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One supplier bill, as entered at the counter. Party and product details are copied onto the
/// document rather than only referenced: a purchase is a tax record, and it must keep showing what
/// was actually billed even after the supplier is renamed or a part's rate changes.
/// </summary>
public class Purchase : AuditableEntity
{
    /// <summary>Internal running number, e.g. <c>PUR/2026-27/0001</c>.</summary>
    public string PurchaseNumber { get; set; } = string.Empty;

    /// <summary>Financial year the number belongs to, e.g. <c>2026-27</c>. Numbering restarts each year.</summary>
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Position within <see cref="FinancialYear"/>. Unique together with it.</summary>
    public int Sequence { get; set; }

    /// <summary>The number printed on the supplier's own bill. This is what GSTR-2 is reconciled against.</summary>
    public string SupplierInvoiceNumber { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierGstin { get; set; }
    public string? SupplierStateCode { get; set; }

    /// <summary>
    /// Decided once, when the document is created, by comparing the supplier's state code against
    /// the shop's. Stored rather than recomputed so a later change to either party cannot
    /// retroactively flip a filed document between IGST and CGST+SGST.
    /// </summary>
    public bool IsInterState { get; set; }

    /// <summary>
    /// Number of lines, denormalised. The list screen shows it on every row, and a document's lines
    /// never change after it is created, so counting it once beats joining the line table for a page
    /// of results.
    /// </summary>
    public int ItemCount { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTax { get; set; }

    /// <summary>Difference between the computed total and the rupee it is billed at. Can be negative.</summary>
    public decimal RoundOff { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Grand total less amount paid. Stored rather than derived, for the same reason
    /// <see cref="Invoice.BalanceDue"/> is: once payments can move it, "which bills are still open"
    /// has to be an indexed query rather than an expression.
    /// </summary>
    public decimal BalanceDue { get; set; }

    /// <summary>The mirror of <see cref="Invoice.CreditAppliedAmount"/>, moved by debit notes.</summary>
    public decimal DebitAppliedAmount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Credit;

    public string? Notes { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Received;

    public List<PurchaseItem> Items { get; set; } = [];
}

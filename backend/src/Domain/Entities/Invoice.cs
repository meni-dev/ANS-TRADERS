using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A tax invoice issued to a customer. Like <see cref="Purchase"/>, party details are snapshotted
/// onto the document — an invoice already handed to a customer must keep reading exactly as it was
/// printed.
/// </summary>
public class Invoice : AuditableEntity
{
    /// <summary>Running number printed on the bill, e.g. <c>INV/2026-27/0001</c>.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Financial year the number belongs to, e.g. <c>2026-27</c>. Numbering restarts each year.</summary>
    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>Position within <see cref="FinancialYear"/>. Unique together with it.</summary>
    public int Sequence { get; set; }

    public DateOnly InvoiceDate { get; set; }

    /// <summary>
    /// When payment is expected — invoice date plus the customer's agreed credit days. Null on a
    /// bill raised before terms were captured, and ageing falls back to the invoice date for those,
    /// so historical rows behave exactly as they always have.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Null for an unregistered walk-in paying cash, where only a name is captured.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerGstin { get; set; }
    public string? CustomerStateCode { get; set; }

    /// <summary>See the note on <see cref="Purchase.IsInterState"/>.</summary>
    public bool IsInterState { get; set; }

    /// <summary>See the note on <see cref="Purchase.ItemCount"/>.</summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// A discount given on the bill as a whole — the counter's "make it ₹950". Recorded as it was
    /// entered, and spread across the lines before tax so GST is charged on what was actually taken.
    /// </summary>
    public decimal BillDiscountPercent { get; set; }

    /// <summary>The rupee value of <see cref="BillDiscountPercent"/>, or a flat amount entered directly.</summary>
    public decimal BillDiscountAmount { get; set; }

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

    /// <summary>Grand total less amount paid. Stored so the outstanding list is a plain query.</summary>
    public decimal BalanceDue { get; set; }

    /// <summary>
    /// How much of this bill has been credited back by credit notes, capped at what it still had
    /// outstanding when each note was raised. The third term in the balance identity:
    /// <c>BalanceDue = GrandTotal − AmountPaid − CreditAppliedAmount</c>, enforced by a database
    /// check so no code path can move one without the others.
    /// <para>
    /// Named for what it does rather than what it is: on a bill already settled in full, goods can
    /// still be <i>credited</i> to the customer while nothing at all is <i>applied</i> to the bill —
    /// that credit lands on their account instead.
    /// </para>
    /// </summary>
    public decimal CreditAppliedAmount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    public string? Notes { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    public List<InvoiceItem> Items { get; set; } = [];
}

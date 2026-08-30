using Domain.Common;

using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One line of a credit note — some or all of one invoice line, coming back.
/// <para>
/// Every snapshot here is copied from the <b>invoice line</b>, not from the product master. That is
/// a deliberate departure from <c>InvoiceService.CreateAsync</c>, which reads the master: if a part
/// was renamed or re-rated since the sale, this note must still read as the bill it credits.
/// </para>
/// </summary>
public class CreditNoteItem : Entity
{
    public Guid CreditNoteId { get; set; }

    /// <summary>
    /// The invoice line being reversed. What the over-return guard counts against, and what ties a
    /// returned quantity back to the quantity actually sold.
    /// </summary>
    public Guid InvoiceItemId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string PartNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Hsn { get; set; } = string.Empty;
    public string Uqc { get; set; } = "PCS";

    /// <summary>How much came back. Always positive; the direction lives in the document type.</summary>
    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    /// <summary>
    /// Copied from the invoice line being reversed, not from today's master — the goods going back
    /// on the shelf are worth what they cost when they left it. See
    /// <see cref="InvoiceItem.CostRate"/>.
    /// </summary>
    public decimal? CostRate { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }

    /// <summary>
    /// Copied from the line being credited, not read back from the product master.
    /// <para>
    /// A rate of zero cannot say on its own what kind of supply it is — a taxable part priced at
    /// nothing and a nil-rated one both read 0. GSTR-1 Table 8 and GSTR-3B 3.1(c) need to know
    /// which, so the answer travels with the note the way it already travels with the bill.
    /// </para>
    /// </summary>
    public SupplyType SupplyType { get; set; }

    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal LineTotal { get; set; }
}

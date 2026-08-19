using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// One line of a debit note. Mirrors <see cref="CreditNoteItem"/>, including the rule that every
/// snapshot comes from the original bill's line rather than from the product master.
/// </summary>
public class DebitNoteItem : Entity
{
    public Guid DebitNoteId { get; set; }

    /// <summary>The purchase line being reversed — what the over-return guard counts against.</summary>
    public Guid PurchaseItemId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string PartNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Hsn { get; set; } = string.Empty;
    public string Uqc { get; set; } = "PCS";

    /// <summary>How much went back. Always positive; the direction lives in the document type.</summary>
    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal LineTotal { get; set; }
}

using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A single line on a supplier bill. The product columns are a snapshot taken at entry time —
/// see the note on <see cref="Purchase"/>.
/// </summary>
public class PurchaseItem : Entity
{
    public Guid PurchaseId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string PartNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Hsn { get; set; } = string.Empty;
    public string Uqc { get; set; } = "PCS";

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }

    /// <summary>
    /// How much of this line has already gone back on live debit notes. What the over-return guard
    /// reads, and what the return screen shows as "3 of 5 already returned" the moment the document
    /// is opened.
    /// </summary>
    public decimal ReturnedQuantity { get; set; }

    public decimal DiscountPercent { get; set; }

    /// <summary>Derived from <see cref="DiscountPercent"/>. Stored so the printed bill never has to recompute it.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Quantity × rate, before discount.</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Gross less discount. The base every tax figure on this line is a percentage of.</summary>
    public decimal TaxableAmount { get; set; }

    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }

    /// <summary>Taxable plus tax.</summary>
    public decimal LineTotal { get; set; }
}

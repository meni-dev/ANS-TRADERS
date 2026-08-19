using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A single line on a tax invoice. Mirrors <see cref="PurchaseItem"/> field for field so the two
/// documents can share one calculator and one printed layout.
/// </summary>
public class InvoiceItem : Entity
{
    public Guid InvoiceId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string PartNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Hsn { get; set; } = string.Empty;
    public string Uqc { get; set; } = "PCS";

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }

    /// <summary>
    /// What this stock cost the shop, frozen at the moment of sale.
    /// <para>
    /// Snapshotted rather than read from the product master at report time, because
    /// <see cref="Product.PurchaseRate"/> moves every time the supplier's price does — valuing last
    /// month's sale at this month's cost is how a margin figure quietly becomes fiction. It cannot
    /// be filled in later either: once the rate has moved, what the goods actually cost on the day
    /// is gone.
    /// </para>
    /// <para>
    /// Deliberately the product's purchase rate and not a weighted-average costing engine. For one
    /// shop that is honest and an order of magnitude simpler; its limit is that stock bought at an
    /// older price and sold after a rate change carries the newer cost.
    /// </para>
    /// <para>
    /// <b>Nullable on purpose.</b> Lines sold before cost was captured have no honest value to put
    /// here, and zero would read as "these goods were free" — a 100% margin on every historical
    /// bill. Null says <i>not known</i>, so margin reports can exclude those lines and say how many
    /// they excluded rather than quietly overstating what the shop earned.
    /// </para>
    /// </summary>
    public decimal? CostRate { get; set; }

    /// <summary>
    /// How much of this line has already gone back on live credit notes. What the over-return guard
    /// reads, and what the return screen shows as "3 of 5 already returned" the moment the document
    /// is opened.
    /// </summary>
    public decimal ReturnedQuantity { get; set; }

    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// This line's slice of the bill-level discount, already deducted from
    /// <see cref="TaxableAmount"/>. Stored so the arithmetic can be explained, and so a return of
    /// this line credits what was actually charged rather than the pre-discount rate.
    /// </summary>
    public decimal BillDiscountShare { get; set; }

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

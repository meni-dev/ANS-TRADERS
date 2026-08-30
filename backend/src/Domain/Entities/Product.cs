using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Product : AuditableEntity
{
    public string ItemCode { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public string Hsn { get; set; } = string.Empty;
    public decimal GstRate { get; set; }

    /// <summary>
    /// Which GSTR-1 table this part belongs in. Taxable for almost everything; the others exist
    /// because a rate of zero alone cannot say whether goods are nil rated, exempt or outside GST.
    /// </summary>
    public SupplyType SupplyType { get; set; } = SupplyType.Taxable;

    /// <summary>Half of <see cref="GstRate"/>. Derived server-side, never accepted from the client.</summary>
    public decimal CgstRate { get; set; }

    /// <summary>Half of <see cref="GstRate"/>. Derived server-side, never accepted from the client.</summary>
    public decimal SgstRate { get; set; }

    public string Uqc { get; set; } = "PCS";
    public decimal PurchaseRate { get; set; }
    public decimal SellingRate { get; set; }
    public decimal Mrp { get; set; }
    /// <summary>Stock on the shelf when the item started being tracked. Set once, at creation.</summary>
    public decimal OpeningStock { get; set; }

    /// <summary>
    /// Current quantity on the shelf. A running total of <see cref="StockMovement"/>, denormalised
    /// here because billing checks it on every line and summing the ledger for each keystroke would
    /// not hold up at a counter.
    /// </summary>
    public decimal StockOnHand { get; set; }

    /// <summary>
    /// Quantity at or below which the item counts as low and needs reordering. Zero means the item
    /// is only flagged once it is actually out.
    /// </summary>
    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;
}
